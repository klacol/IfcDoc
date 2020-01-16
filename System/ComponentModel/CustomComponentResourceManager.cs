namespace System.ComponentModel
{
	internal class CustomComponentResourceManager : ComponentResourceManager
	{
		public CustomComponentResourceManager(Type type, string resourceName)
		   : base(type)
		{
			this.BaseNameField = resourceName;
		}
	}
}