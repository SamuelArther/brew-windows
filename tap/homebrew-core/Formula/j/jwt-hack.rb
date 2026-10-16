class JwtHack < Formula
  desc "JSON Web Token Hack Toolkit"
  homepage "https://github.com/hahwul/jwt-hack"
  url "https://github.com/hahwul/jwt-hack/releases/download/v2.6.0/jwt-hack-v2.6.0-windows-x86_64.zip"
  sha256 "8782a15db7131ba0a5e621981ecaf67c7189300c5b2ea98f1c55bb85a76cb610"
  license "MIT"

  # Official Windows x64 release binary from the project's GitHub releases.

  livecheck do
    url "https://github.com/hahwul/jwt-hack/archive/refs/tags/v2.6.0.tar.gz"
  end

  def install
    bin.install "jwt-hack.exe"
  end

  test do
    assert_match version.to_s, shell_output("#{bin}/jwt-hack --version 2>&1")
  end
end
