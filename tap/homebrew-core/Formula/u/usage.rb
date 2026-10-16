class Usage < Formula
  desc "Tool for working with usage-spec CLIs"
  homepage "https://usage.jdx.dev/"
  url "https://github.com/jdx/usage/releases/download/v7.0.1/usage-x86_64-pc-windows-msvc.zip"
  sha256 "9518804218a48901ac82228618ca58503d7d79166e61df6c04fa8e6a07ef7666"
  license "MIT"

  # Official Windows x64 release binary from the project's GitHub releases.

  livecheck do
    url "https://github.com/jdx/usage/archive/refs/tags/v7.0.1.tar.gz"
  end

  def install
    bin.install "usage.exe"
  end

  test do
    assert_match version.to_s, shell_output("#{bin}/usage --version 2>&1")
  end
end
