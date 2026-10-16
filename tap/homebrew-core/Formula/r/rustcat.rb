class Rustcat < Formula
  desc "Modern Port listener and Reverse shell"
  homepage "https://github.com/robiot/rustcat"
  url "https://github.com/robiot/rustcat/releases/download/v3.0.0/rcat-v3.0.0-win-x86_64.exe"
  sha256 "0f82d74827913f91363f25148c2767ae6273fd07256f5999f5e801325348487b"
  license "GPL-3.0-only"

  # Official Windows x64 release binary from the project's GitHub releases.

  livecheck do
    url "https://github.com/robiot/rustcat/archive/refs/tags/v3.0.0.tar.gz"
  end

  def install
    bin.install "rcat-v3.0.0-win-x86_64.exe" => "rcat.exe"
  end

  test do
    assert_match version.to_s, shell_output("#{bin}/rcat --version 2>&1")
  end
end
