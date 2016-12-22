﻿using System;
using System.Collections.Generic;

namespace Tests.Diagnostics.ComparableInterfaceImplementation
{
    public class Compliant : IComparable // Compliant
    {
        public string Name { get; set; }

        public int CompareTo(object obj)
        {
            return Compare(this, obj as Compliant);
        }

        private static int Compare(Compliant left, Compliant right)
        {
            if (left == null && right == null)
            {
                return 0;
            }

            if (left == null)
            {
                return -1;
            }

            if (right == null)
            {
                return 1;
            }

            return left.Name.CompareTo(right.Name);
        }

        public override bool Equals(object obj)
        {
            var other = obj as Compliant;
            return other != null && this.Name != other.Name;
        }

        public static bool operator ==(Compliant left, Compliant right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(Compliant left, Compliant right)
        {
            return !(left == right);
        }

        public static bool operator <(Compliant left, Compliant right)
        {
            return Compare(left, right) < 0;
        }

        public static bool operator >(Compliant left, Compliant right)
        {
            return Compare(left, right) > 0;
        }
    }

    public class DerivedCompliant : Compliant, IComparable // Compliant
    {
    }

    public class MissingEquals : IComparable // Noncompliant {{When implementing IComparable, you should also override Equals.}}
//               ^^^^^^^^^^^^^
    {
        public string Name { get; set; }

        public int CompareTo(object obj)
        {
            return Compare(this, obj as MissingEquals);
        }

        private static int Compare(MissingEquals left, MissingEquals right)
        {
            if (left == null && right == null)
            {
                return 0;
            }

            if (left == null)
            {
                return -1;
            }

            if (right == null)
            {
                return 1;
            }

            return left.Name.CompareTo(right.Name);
        }

        public static bool operator ==(MissingEquals left, MissingEquals right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(MissingEquals left, MissingEquals right)
        {
            return !(left == right);
        }

        public static bool operator <(MissingEquals left, MissingEquals right)
        {
            return Compare(left, right) < 0;
        }

        public static bool operator >(MissingEquals left, MissingEquals right)
        {
            return Compare(left, right) > 0;
        }
    }

    public class DiferentEquals : IComparable // Noncompliant {{When implementing IComparable, you should also override Equals.}}
//               ^^^^^^^^^^^^^^
    {
        public string Name { get; set; }

        public int CompareTo(object obj)
        {
            return Compare(this, obj as DiferentEquals);
        }

        private static int Compare(DiferentEquals left, DiferentEquals right)
        {
            if (left == null && right == null)
            {
                return 0;
            }

            if (left == null)
            {
                return -1;
            }

            if (right == null)
            {
                return 1;
            }

            return left.Name.CompareTo(right.Name);
        }

        public bool Equals(object obj, string someParam)
        {
            var other = obj as DiferentEquals;
            return other != null && this.Name != other.Name;
        }

        public void Equals(object obj)
        {
        }

        public bool Equals()
        {
        }

        public bool Equals { get; set; }

        public bool Equals => true;

        public bool Equals() => true;

        public static bool operator ==(DiferentEquals left, DiferentEquals right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(DiferentEquals left, DiferentEquals right)
        {
            return !(left == right);
        }

        public static bool operator <(DiferentEquals left, DiferentEquals right)
        {
            return Compare(left, right) < 0;
        }

        public static bool operator >(DiferentEquals left, DiferentEquals right)
        {
            return Compare(left, right) > 0;
        }
    }

    public class MissingGreaterThan : IComparable // Noncompliant {{When implementing IComparable, you should also override <, >.}}
//               ^^^^^^^^^^^^^^^^^^
    {
        public string Name { get; set; }

        public int CompareTo(object obj)
        {
            return Compare(this, obj as MissingGreaterThan);
        }

        private static int Compare(MissingGreaterThan left, MissingGreaterThan right)
        {
            if (left == null && right == null)
            {
                return 0;
            }

            if (left == null)
            {
                return -1;
            }

            if (right == null)
            {
                return 1;
            }

            return left.Name.CompareTo(right.Name);
        }

        public override bool Equals(object obj)
        {
            var other = obj as MissingGreaterThan;
            return other != null && this.Name != other.Name;
        }

        public static bool operator ==(MissingGreaterThan left, MissingGreaterThan right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(MissingGreaterThan left, MissingGreaterThan right)
        {
            return !(left == right);
        }
    }

    public class MissingNotEqual : IComparable // Noncompliant {{When implementing IComparable, you should also override ==, !=.}}
//               ^^^^^^^^^^^^^^^
    {
        public string Name { get; set; }

        public int CompareTo(object obj)
        {
            return Compare(this, obj as MissingNotEqual);
        }

        private static int Compare(MissingNotEqual left, MissingNotEqual right)
        {
            if (left == null && right == null)
            {
                return 0;
            }

            if (left == null)
            {
                return -1;
            }

            if (right == null)
            {
                return 1;
            }

            return left.Name.CompareTo(right.Name);
        }

        public override bool Equals(object obj)
        {
            var other = obj as MissingNotEqual;
            return other != null && this.Name != other.Name;
        }

        public static bool operator <(MissingNotEqual left, MissingNotEqual right)
        {
            return Compare(left, right) < 0;
        }

        public static bool operator >(MissingNotEqual left, MissingNotEqual right)
        {
            return Compare(left, right) > 0;
        }
    }
}

namespace Tests.Diagnostics.ComparableGenericInterfaceImplementation
{
    public class Compliant : IComparable<Compliant> // Compliant
    {
        public string Name { get; set; }

        public int CompareTo(Compliant obj)
        {
            return Compare(this, obj);
        }

        private static int Compare(Compliant left, Compliant right)
        {
            if (left == null && right == null)
            {
                return 0;
            }

            if (left == null)
            {
                return -1;
            }

            if (right == null)
            {
                return 1;
            }

            return left.Name.CompareTo(right.Name);
        }

        public override bool Equals(object obj)
        {
            var other = obj as Compliant;
            return other != null && this.Name != other.Name;
        }

        public static bool operator ==(Compliant left, Compliant right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(Compliant left, Compliant right)
        {
            return !(left == right);
        }

        public static bool operator <(Compliant left, Compliant right)
        {
            return Compare(left, right) < 0;
        }

        public static bool operator >(Compliant left, Compliant right)
        {
            return Compare(left, right) > 0;
        }
    }

    public class MissingEquals : IComparable<MissingEquals> // Noncompliant {{When implementing IComparable<T>, you should also override Equals.}}
//               ^^^^^^^^^^^^^
    {
        public string Name { get; set; }

        public int CompareTo(MissingEquals obj)
        {
            return Compare(this, obj);
        }

        private static int Compare(MissingEquals left, MissingEquals right)
        {
            if (left == null && right == null)
            {
                return 0;
            }

            if (left == null)
            {
                return -1;
            }

            if (right == null)
            {
                return 1;
            }

            return left.Name.CompareTo(right.Name);
        }

        public static bool operator ==(MissingEquals left, MissingEquals right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(MissingEquals left, MissingEquals right)
        {
            return !(left == right);
        }

        public static bool operator <(MissingEquals left, MissingEquals right)
        {
            return Compare(left, right) < 0;
        }

        public static bool operator >(MissingEquals left, MissingEquals right)
        {
            return Compare(left, right) > 0;
        }
    }

    public class DiferentEquals : IComparable<DiferentEquals> // Noncompliant {{When implementing IComparable<T>, you should also override Equals.}}
//               ^^^^^^^^^^^^^^
    {
        public string Name { get; set; }

        public int CompareTo(DiferentEquals obj)
        {
            return Compare(this, obj);
        }

        private static int Compare(DiferentEquals left, DiferentEquals right)
        {
            if (left == null && right == null)
            {
                return 0;
            }

            if (left == null)
            {
                return -1;
            }

            if (right == null)
            {
                return 1;
            }

            return left.Name.CompareTo(right.Name);
        }

        public bool Equals(object obj, string someParam)
        {
            var other = obj as DiferentEquals;
            return other != null && this.Name != other.Name;
        }

        public void Equals(object obj)
        {
        }

        public static bool operator ==(DiferentEquals left, DiferentEquals right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(DiferentEquals left, DiferentEquals right)
        {
            return !(left == right);
        }

        public static bool operator <(DiferentEquals left, DiferentEquals right)
        {
            return Compare(left, right) < 0;
        }

        public static bool operator >(DiferentEquals left, DiferentEquals right)
        {
            return Compare(left, right) > 0;
        }
    }

    public class MissingGreaterThan : IComparable<MissingGreaterThan> // Noncompliant {{When implementing IComparable<T>, you should also override <, >.}}
//               ^^^^^^^^^^^^^^^^^^
    {
        public string Name { get; set; }

        public int CompareTo(MissingGreaterThan obj)
        {
            return Compare(this, obj);
        }

        private static int Compare(MissingGreaterThan left, MissingGreaterThan right)
        {
            if (left == null && right == null)
            {
                return 0;
            }

            if (left == null)
            {
                return -1;
            }

            if (right == null)
            {
                return 1;
            }

            return left.Name.CompareTo(right.Name);
        }

        public override bool Equals(object obj)
        {
            var other = obj as MissingGreaterThan;
            return other != null && this.Name != other.Name;
        }

        public static bool operator ==(MissingGreaterThan left, MissingGreaterThan right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(MissingGreaterThan left, MissingGreaterThan right)
        {
            return !(left == right);
        }
    }

    public class MissingNotEqual : IComparable<MissingNotEqual> // Noncompliant {{When implementing IComparable<T>, you should also override ==, !=.}}
//               ^^^^^^^^^^^^^^^
    {
        public string Name { get; set; }

        public int CompareTo(MissingNotEqual obj)
        {
            return Compare(this, obj);
        }

        private static int Compare(MissingNotEqual left, MissingNotEqual right)
        {
            if (left == null && right == null)
            {
                return 0;
            }

            if (left == null)
            {
                return -1;
            }

            if (right == null)
            {
                return 1;
            }

            return left.Name.CompareTo(right.Name);
        }

        public override bool Equals(object obj)
        {
            var other = obj as MissingNotEqual;
            return other != null && this.Name != other.Name;
        }

        public static bool operator <(MissingNotEqual left, MissingNotEqual right)
        {
            return Compare(left, right) < 0;
        }

        public static bool operator >(MissingNotEqual left, MissingNotEqual right)
        {
            return Compare(left, right) > 0;
        }
    }
}

namespace Tests.Diagnostics.BothInterfacesImplementation
{
    public class NonCompliant : IComparable, IComparable<NonCompliant> // Noncompliant {{When implementing IComparable or IComparable<T>, you should also override Equals, <, >, ==, !=.}}
    {
        public string Name { get; set; }

        public int CompareTo(NonCompliant obj)
        {
            return Compare(this, obj);
        }

        private static int Compare(NonCompliant left, NonCompliant right)
        {
            if (left == null && right == null)
            {
                return 0;
            }

            if (left == null)
            {
                return -1;
            }

            if (right == null)
            {
                return 1;
            }

            return left.Name.CompareTo(right.Name);
        }
    }
}