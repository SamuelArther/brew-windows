class Cosign < Formula
  desc "Container Signing"
  homepage "https://github.com/sigstore/cosign"
  url "https://github.com/sigstore/cosign/releases/download/v3.1.3/cosign-windows-amd64.exe"
  sha256 "9fe59be0eca1271873ce019061335eb1ac419b7059202e797828467ddabe33be"
  license "Apache-2.0"

  livecheck do
    url :stable
    strategy :github_latest
  end

  def install
    bin.install "cosign-windows-amd64.exe" => "cosign.exe"
  end

  test do
    assert_match version.to_s, shell_output("#{bin}/cosign version 2>&1")
  end
end
