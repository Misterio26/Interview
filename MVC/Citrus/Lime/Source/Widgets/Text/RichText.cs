using System;
using System.Collections.Generic;
using Lime.Text;
using Yuzu;

namespace Lime
{
	[TangerineRegisterNode(Order = 11)]
	[TangerineNodeBuilder("BuildForTangerine")]
	[TangerineAllowedChildrenTypes(typeof(TextStyle))]
	[TangerineVisualHintGroup("/All/Nodes/Text")]
	public class RichText : Widget, IText
	{
		private TextParser parser = new TextParser();
		private string text;
		private HAlignment hAlignment;
		private VAlignment vAlignment;
		private TextOverflowMode overflowMode;
		private SpriteList[] spriteLists;
		private TextRenderer renderer;
		private string displayText;
		private TextProcessorDelegate textProcessor;
		private int maxDisplayCharacters = -1;
		private int[] gradientMapIndices;

		/// <summary>
		/// Is invoked for an each text assigned to an any RichText instance.
		/// Does not invalidate already assigned text.
		/// </summary>
		public static event TextProcessorDelegate GlobalTextProcessor;

		[YuzuMember]
		[TangerineKeyframeColor(12)]
		public override string Text
		{
			get { return text; }
			set
			{
				if (text != value) {
					text = value;
					Invalidate();
				}
			}
		}

		public string DisplayText
		{
			get
			{
				if (displayText == null) {
					displayText = Localizable ? Text.Localize() : Text;
					GlobalTextProcessor?.Invoke(ref displayText, this);
					textProcessor?.Invoke(ref displayText, this);
				}
				return displayText;
			}
		}

		public event TextProcessorDelegate TextProcessor
		{
			add
			{
				textProcessor += value;
				Invalidate();
			}
			remove
			{
				textProcessor -= value;
				Invalidate();
			}
		}

		[YuzuMember]
		[TangerineKeyframeColor(13)]
		public HAlignment HAlignment
		{
			get { return hAlignment; }
			set
			{
				if (hAlignment != value) {
					hAlignment = value;
					Invalidate();
				}
			}
		}

		[YuzuMember]
		[TangerineKeyframeColor(14)]
		public VAlignment VAlignment
		{
			get { return vAlignment; }
			set
			{
				if (vAlignment != value) {
					vAlignment = value;
					Invalidate();
				}
			}
		}

		[YuzuMember]
		[TangerineKeyframeColor(15)]
		public TextOverflowMode OverflowMode
		{
			get { return overflowMode; }
			set
			{
				if (overflowMode != value) {
					overflowMode = value;
					Invalidate();
				}
			}
		}

		[YuzuMember]
		[TangerineKeyframeColor(16)]
		public bool WordSplitAllowed { get; set; }

		// TODO
		public bool TrimWhitespaces { get; set; }

		public event Action<string> Submitted;

		public RichText()
		{
			Presenter = DefaultPresenter.Instance;
			Localizable = true;
			Localizable = true;
			TrimWhitespaces = true;
			Text = "";
		}

		void IText.Submit()
		{
			if (Submitted != null) {
				Submitted(Text);
			}
		}

		public override void Dispose()
		{
			Invalidate();
			base.Dispose();
		}

		private string errorMessage;

		public string ErrorMessage
		{
			get
			{
				ParseText();
				return errorMessage;
			}
		}

		protected override void OnSizeChanged(Vector2 sizeDelta)
		{
			base.OnSizeChanged(sizeDelta);
			Invalidate();
		}

		private bool InvertStylesPalleteIndex()
		{
			bool dirty = false;
			foreach (var s in renderer.Styles) {
				if (s.GradientMapIndex != -1) {
					dirty = true;
					s.gradientMapIndex = ShaderPrograms.ColorfulTextShaderProgram.GradientMapTextureSize - s.GradientMapIndex - 1;
				}
			}
			return dirty;
		}

		public override void AddToRenderChain(RenderChain chain)
		{
			if (GloballyVisible && ClipRegionTest(chain.ClipRegion)) {
				AddSelfAndChildrenToRenderChain(chain, Layer);
			}
		}

		protected override void DiscardMaterial()
		{
			Invalidate();
		}

		protected internal override Lime.RenderObject GetRenderObject()
		{
			EnsureSpriteLists();
			var ro = RenderObjectPool<RenderObject>.Acquire();
			//ro.CaptureRenderState causes RichText invalidation on every frame,
			//so use local values for blending and shader
			ro.LocalToWorldTransform = LocalToWorldTransform;
			ro.Blending = Blending;
			ro.Shader = Shader;
			ro.Objects = new RenderObjectList();
			var scale = Mathf.Sqrt(Math.Max(LocalToWorldTransform.U.SqrLength, LocalToWorldTransform.V.SqrLength));
			for (int i = 0; i < renderer.Styles.Count; i++) {
				var style = renderer.Styles[i];
				var sdfComponent = style.Components.Get<SignedDistanceFieldComponent>();
				if (sdfComponent != null) {
					var sdfRO = sdfComponent.GetRenderObject();
					foreach (var obj in sdfRO.Objects) {
						obj.SpriteList = spriteLists[i];
						obj.Color = GlobalColor;
						obj.LocalToWorldTransform = LocalToWorldTransform;
						obj.Shader = Shader;
						obj.Blending = Blending;
					}
					ro.Objects.Add(sdfRO);
				} else {
					var styleRO = RenderObjectPool<TextRenderObject>.Acquire();
					styleRO.SpriteList = spriteLists[i];
					styleRO.RenderMode = TextRenderingMode.TwoPasses;
					styleRO.Color = GlobalColor;
					styleRO.GradientMapIndex = style.GradientMapIndex;
					styleRO.LocalToWorldTransform = LocalToWorldTransform;
					styleRO.Shader = Shader;
					styleRO.Blending = Blending;
					ro.Objects.Add(styleRO);
				}
			}
			return ro;
		}

		private void EnsureSpriteLists()
		{
			if (spriteLists == null) {
				PrepareRenderer();
				spriteLists = new SpriteList[renderer.Styles.Count];
				for (int i = 0; i < spriteLists.Length; i++) {
					spriteLists[i] = new SpriteList();
				}
				renderer.Render(spriteLists, Size, HAlignment, VAlignment, maxDisplayCharacters);
				gradientMapIndices = new int[renderer.Styles.Count];
				for (var i = 0; i < gradientMapIndices.Length; i++) {
					gradientMapIndices[i] = renderer.Styles[i].GradientMapIndex;
				}
			}
		}

		// TODO: return effective AABB, not only extent
		public Rectangle MeasureText()
		{
			var extent = PrepareRenderer().MeasureText(Size.X, Size.Y);
			return new Rectangle(Vector2.Zero, extent);
		}

		// TODO
		public bool Localizable { get; set; }

		public override void StaticScale(float ratio, bool roundCoordinates)
		{
			foreach (var node in Nodes) {
				var style = node as TextStyle;
				if (style != null) {
					style.Size *= ratio;
					style.ImageSize *= ratio;
					style.SpaceAfter *= ratio;
					style.ShadowOffset *= ratio;
				}
			}
			base.StaticScale(ratio, roundCoordinates);
		}

		private TextRenderer PrepareRenderer()
		{
			if (renderer != null)
				return renderer;
			ParseText();
			renderer = new TextRenderer(OverflowMode, WordSplitAllowed);
			// Setup default style(take first one from node list or TextStyle.Default).
			TextStyle defaultStyle = null;
			if (Nodes.Count > 0) {
				defaultStyle = Nodes[0] as TextStyle;
			}
			renderer.AddStyle(defaultStyle ?? TextStyle.Default);
			// Fill up style list.
			foreach (var styleName in parser.Styles) {
				var style = Nodes.TryFind(styleName) as TextStyle;
				renderer.AddStyle(style ?? TextStyle.Default);
			}
			// Add text fragments.
			foreach (var frag in parser.Fragments) {
				// Warning! Using style + 1, because -1 is a default style.
				renderer.AddFragment(frag.Text, frag.Style + 1, frag.IsNbsp);
			}
			return renderer;
		}

		public void RemoveUnusedStyles()
		{
			PrepareRenderer();
			for (int i = 1; i < Nodes.Count; ) {
				var node = Nodes[i];
				if (node is TextStyle && !renderer.HasStyle(node as TextStyle))
					Nodes.RemoveAt(i);
				else
					++i;
			}
		}

		private void ParseText()
		{
			if (parser != null) {
				return;
			}
			parser = new TextParser(DisplayText);
			errorMessage = parser.ErrorMessage;
			if (errorMessage != null) {
				parser = new TextParser("Error: " + errorMessage);
			}
		}

		public void Invalidate()
		{
			spriteLists = null;
			gradientMapIndices = null;
			parser = null;
			renderer = null;
			displayText = null;
			InvalidateParentConstraintsAndArrangement();
			Window.Current?.Invalidate();
		}

		/// Call on user-supplied parts of text.
		public static string Escape(string text)
		{
			return text.
				Replace("&amp;", "&amp;amp;").Replace("&lt;", "&amp;lt;").Replace("&gt;", "&amp;&gt").
				Replace("<", "&lt;").Replace(">", "&gt;");
		}

		/// <summary>
		/// Make a hit test. Returns style of a text chunk a user hit to.
		/// TODO: implement more advanced system with tag attributes like <link url="...">...</link>
		/// </summary>
		public bool HitTest(Vector2 point, out TextStyle style)
		{
			style = null;
			int tag;
			EnsureSpriteLists();
			foreach (var spriteList in spriteLists) {
				if (spriteList.HitTest(LocalToWorldTransform, point, out tag) && tag >= 0) {
					style = null;
					if (tag == 0) {
						style = Nodes[0] as TextStyle;
					} else {
						style = Nodes.TryFind(parser.Styles[tag - 1]) as TextStyle;
					}
					return true;
				}
			}
			return false;
		}

		public ICaretPosition Caret { get; set; } = DummyCaretPosition.Instance;

		void IText.SyncCaretPosition() { }

		public bool CanDisplay(char ch) { return true; }

		private void BuildForTangerine()
		{
			var style = new TextStyle { Id = "TextStyle1" };
			Nodes.Add(style);
		}

		public int CalcNumCharacters()
		{
			return PrepareRenderer().CalcNumCharacters(Size);
		}

		public int MaxDisplayCharacters {
			get {
				return maxDisplayCharacters;
			}
			set {
				// buz: ????????????????????????????, ?????? ?????? ???????????????? ???????????????????????? ?????????? ?????? ????????????, ?????????? ??????????????
				// ???????????? "????????????????????????????" ????????????????, ?????????????? ???? ???????????? ???????????? ?????? ???????????? Invalidate.
				if (maxDisplayCharacters != value) {
					maxDisplayCharacters = value;
					spriteLists = null;
				}
			}
		}

		internal class RenderObject : WidgetRenderObject
		{
			public RenderObjectList Objects;

			public override void Render()
			{
				Renderer.Transform1 = LocalToWorldTransform;
				foreach (var ro in Objects) {
					ro.Render();
				}
			}

			protected override void OnRelease()
			{
				foreach (var ro in Objects) {
					ro.Release();
				}
			}
		}

		private class ColorfulMaterialProvider : Sprite.IMaterialProvider
		{
			public static readonly ColorfulMaterialProvider Instance = new ColorfulMaterialProvider();

			private ShaderId shader;
			private Blending blending;
			private IMaterial material;
			private int gradientMapIndex;
			private int[] gradientMapIndices;

			public void Init(Blending blending, ShaderId shader, int[] gradientMapIndices)
			{
				this.blending = blending;
				this.shader = shader;
				this.gradientMapIndices = gradientMapIndices;
				material = null;
			}

			public IMaterial GetMaterial(int tag)
			{
				var styleGradientMapIndex = gradientMapIndices[tag];
				if (material == null || gradientMapIndex != styleGradientMapIndex) {
					gradientMapIndex = styleGradientMapIndex;
					if (gradientMapIndex < 0) {
						material = WidgetMaterial.GetInstance(blending, shader, 1);
					} else {
						material = ColorfulTextMaterial.GetInstance(blending, gradientMapIndex);
					}
				}
				return material;
			}

			public Sprite ProcessSprite(Sprite s) => s;
		}
	}
}
