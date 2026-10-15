class Re2c < Formula
  desc "Generate C-based recognizers from regular expressions"
  homepage "https://re2c.org/"
  url "https://repo.msys2.org/msys/x86_64/re2c-4.6-1-x86_64.pkg.tar.zst"
  sha256 "7e156d33237382e8a553ba0cd0b21c915e85c370b0abe9b1513b200ae5816b7d"
  license :public_domain

  # Prebuilt package from MSYS2 (an isolated root per keg; MSYS2 path rules apply).

  livecheck do
    url "https://github.com/skvadrik/re2c/releases/download/4.6/re2c-4.6.tar.xz"
  end

  resource "gcc-libs" do
    url "https://repo.msys2.org/msys/x86_64/gcc-libs-15.3.0-1-x86_64.pkg.tar.zst"
    sha256 "0d99a122c453c05ae21ba3dcea910f2e0d93d38ae067c677a112a315b0f3cec5"
  end

  def install
    msys2_install
    msys2_bin "usr/bin/re2c.exe", "re2c"
    msys2_bin "usr/bin/re2d.exe", "re2d"
    msys2_bin "usr/bin/re2go.exe", "re2go"
    msys2_bin "usr/bin/re2hs.exe", "re2hs"
    msys2_bin "usr/bin/re2js.exe", "re2js"
    msys2_bin "usr/bin/re2ocaml.exe", "re2ocaml"
    msys2_bin "usr/bin/re2py.exe", "re2py"
    msys2_bin "usr/bin/re2rust.exe", "re2rust"
    msys2_bin "usr/bin/re2swift.exe", "re2swift"
    msys2_bin "usr/bin/re2v.exe", "re2v"
    msys2_bin "usr/bin/re2zig.exe", "re2zig"
  end

  test do
    (testpath/"test.c").write <<~C
      unsigned int stou (const char * s)
      {
      #   define YYCTYPE char
          const YYCTYPE * YYCURSOR = s;
          unsigned int result = 0;

          for (;;)
          {
              /*!re2c
                  re2c:yyfill:enable = 0;

                  "\x00" { return result; }
                  [0-9]  { result = result * 10 + c; continue; }
              */
          }
      }
    C
    system bin/"re2c", "-is", testpath/"test.c"
  end
end
