namespace mini
{
	/// <summary>
	/// Class C1.
	/// </summary>
	public class C1
	{
		public void Foo(int i)
        {
			Bar(i);
        }

        /// <summary>
        /// No comment.
        /// </summary>
        public void Bar(int i)
		{
			// FIXME
			C2 c2 = new C2();
			c2.attr = i;
		}
	}
}
