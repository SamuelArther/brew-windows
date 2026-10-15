class Gpatch < Formula
  desc "Apply a diff file to an original"
  homepage "https://savannah.gnu.org/projects/patch/"
  url "https://repo.msys2.org/msys/x86_64/patch-2.8-2-x86_64.pkg.tar.zst"
  sha256 "df34ebf345d607cabf0b2cbad60f562f775f07985647b84035b40702f62ee9b1"
  license "GPL-3.0-or-later"

  # GNU patch via MSYS2, exposed as patch and gpatch.

  resource "msys2-runtime" do
    url "https://repo.msys2.org/msys/x86_64/msys2-runtime-3.6.11-3-x86_64.pkg.tar.zst"
    sha256 "b6478bd1b4cb4b2e07c45db725fee93f9424c19fa248240dcb7b7f4187cd92db"
  end

  def install
    msys2_install
    msys2_bin "usr/bin/patch.exe", "patch"
    msys2_bin "usr/bin/patch.exe", "gpatch"
  end

  test do
    testfile = testpath/"test"
    testfile.write "homebrew\n"
    patch = <<~EOS
      --- test	2008-12-31 00:00:00.000000000 +0000
      +++ test	2008-12-31 00:00:00.000000000 +0000
      @@ -1 +1 @@
      -homebrew
      +hello
    EOS
    pipe_output("#{bin}/gpatch", patch)
    assert_equal "hello", testfile.read.chomp
  end
end
