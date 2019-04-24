using System.Diagnostics;

namespace RefVsValue
{
	internal class Program
	{

		public class RefType : IWithProperty
		{
			public int Field;

			public int Property
			{
				get => Field;
				set => Field = value;
			}
		}

		public struct ValueType : IWithProperty
		{
			public int Field;

			public int Property
			{
				get => Field;
				set => Field = value;
			}
		}

		public interface IWithProperty
		{
			int Property { get; set; }
		}

		public static void Main(string[] args)
		{
			var valueType1 = new ValueType();
			Process1(valueType1);
			Debug.WriteLine($"{nameof(valueType1)}.{nameof(valueType1.Field)} = {valueType1.Field}");

			var valueType2 = new ValueType();
			Process2(ref valueType2);
			Debug.WriteLine($"{nameof(valueType2)}.{nameof(valueType2.Field)} = {valueType2.Field}");

			var refType3 = new RefType();
			Process3(refType3);
			Debug.WriteLine($"{nameof(refType3)}.{nameof(refType3.Field)} = {refType3.Field}");

			var refType4 = new RefType();
			Process4(ref refType4);
			Debug.WriteLine($"{nameof(refType4)}.{nameof(refType4.Field)} = {refType4.Field}");

			var valueType5 = new ValueType();
			Process5(valueType5);
			Debug.WriteLine($"{nameof(valueType5)}.{nameof(valueType5.Property)} = {valueType5.Property}");

			var refType5 = new RefType();
			Process5(refType5);
			Debug.WriteLine($"{nameof(refType5)}.{nameof(refType5.Property)} = {refType5.Property}");
		}

		public static void Process1(ValueType valueType)
		{
			valueType.Field = 1;
		}

		public static void Process2(ref ValueType valueType)
		{
			valueType.Field = 2;
		}

		public static void Process3(RefType refType)
		{
			refType.Field = 3;
		}

		public static void Process4(ref RefType refType)
		{
			refType.Field = 4;
		}

		public static void Process5(IWithProperty withProperty)
		{
			withProperty.Property = 5;
		}
	}
}
