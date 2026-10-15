class Po4a < Formula
  desc "Documentation translation maintenance tool"
  homepage "https://po4a.org"
  url "https://repo.msys2.org/msys/x86_64/po4a-0.74-2-any.pkg.tar.zst"
  sha256 "ca4243f644eec9894cda0d41facbc82c164418741c2179df38593f1ffc01bf5c"
  license "GPL-2.0-or-later"

  # Prebuilt package from MSYS2; headers and import libraries go in the keg.

  livecheck do
    url "https://github.com/mquinson/po4a/archive/refs/tags/v0.74.tar.gz"
  end

  def install
    install_tree
    install_library "libexec/usr"
  end

  test do
    assert_path_exists bin/"site_perl/msguntypot"
  end
end
