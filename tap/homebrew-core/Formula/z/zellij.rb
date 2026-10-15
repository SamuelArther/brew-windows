class Zellij < Formula
  desc "Pluggable terminal workspace, with terminal multiplexer as the base feature"
  homepage "https://zellij.dev"
  url "https://github.com/zellij-org/zellij/releases/download/v0.45.1/zellij-x86_64-pc-windows-msvc.zip"
  sha256 "b854e7b223e67d0705c5f685ef50541c38a57981b13624d09c92bed419b6f80d"
  license "MIT"

  livecheck do
    url :stable
    strategy :github_latest
  end

  def install
    bin.install "zellij.exe"
  end

  test do
    assert_match version.to_s, shell_output("#{bin}/zellij --version")
  end
end
