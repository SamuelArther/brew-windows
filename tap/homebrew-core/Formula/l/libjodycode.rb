class Libjodycode < Formula
  desc "Shared code used by several utilities written by Jody Bruchon"
  homepage "https://codeberg.org/jbruchon/libjodycode"
  url "https://codeberg.org/jbruchon/libjodycode/archive/v4.2.tar.gz"
  sha256 "eeb54fc50b9ebec4e43c954704e69f56c104fede67aaee9f375fbecbb87933b0"
  license "MIT"
  compatibility_version 1

  # These files used to be distributed as part of the jdupes formula
  link_overwrite "include/libjodycode.h", "share/man/man7/libjodycode.7", "lib/libjodycode.a"

  deny_network_access!

  bottle do
    sha256 cellar: :any_skip_relocation, x86_64_windows: "6ed9cce74d56dde55269e10659ee50ae8ac53035e1d8a793e9672d12b6b98fd8"
  end

  def install
    system "make"
    system "make", "install", "PREFIX=#{prefix}"
  end

  test do
    (testpath/"test.c").write <<~C
      #include <stdio.h>
      #include <libjodycode.h>

      int main() {
          int a = jc_strncaseeq("foo", "FOO", 3);
          int b = jc_strncaseeq("foo", "bar", 3);
          int c = jc_strneq("foo", "foo", 3);
          int d = jc_strneq("foo", "FOO", 3);
          printf("%d\\n%d\\n%d\\n%d", a, b, c, d);
      }
    C
    system ENV.cc, "test.c", "-I#{include}", "-L#{lib}", "-ljodycode", "-o", "test"
    assert_equal [0, 1, 0, 1], shell_output("./test").lines.map(&:to_i)
  end
end
