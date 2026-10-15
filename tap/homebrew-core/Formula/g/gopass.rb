class Gopass < Formula
  desc "Slightly more awesome Standard Unix Password Manager for Teams"
  homepage "https://www.gopass.pw/"
  url "https://github.com/gopasspw/gopass/releases/download/v1.17.4/gopass-1.17.4-windows-amd64.zip"
  sha256 "8965ee29f9ede2acfde2de6f5564ecc98de9965a612b675acb453c784d847eaa"
  license "MIT"

  # Official Windows x64 release binary from the project's GitHub releases.

  livecheck do
    url "https://github.com/gopasspw/gopass/releases/download/v1.17.4/gopass-1.17.4.tar.gz"
  end

  def install
    bin.install "gopass.exe"
  end

  test do
    assert_match version.to_s, shell_output("#{bin}/gopass --version 2>&1")
  end
end
