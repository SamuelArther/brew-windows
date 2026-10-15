class Pass < Formula
  desc "Password manager"
  homepage "https://www.passwordstore.org/"
  url "https://repo.msys2.org/msys/x86_64/pass-1.7.4-1-any.pkg.tar.zst"
  sha256 "084f298ceaf58aece8554c11459a011e22dd25b74dcf66a4f76fcaf203a2980c"
  license "GPL-2.0-or-later"

  # Prebuilt package from MSYS2; headers and import libraries go in the keg.

  livecheck do
    url :head
    regex(/^v?(\d+(?:\.\d+)+)$/i)
  end

  def install
    install_tree
    install_library "libexec/usr"
  end

  test do
    assert_path_exists bin/"pass"
  end
end
