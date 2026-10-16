class Ali < Formula
  desc "Generate HTTP load and plot the results in real-time"
  homepage "https://github.com/nakabonne/ali"
  url "https://github.com/nakabonne/ali/releases/download/v0.8.0/ali_0.8.0_windows_amd64.tar.gz"
  sha256 "4da3ef9d58bb46ac4a9910a143cf47c2362b3e2250d1bc675857ecb7d8e8ff06"
  license "MIT"

  # Official Windows x64 release binary from the project's GitHub releases.

  livecheck do
    url "https://github.com/nakabonne/ali/archive/refs/tags/v0.8.0.tar.gz"
  end

  def install
    bin.install "ali.exe"
  end

  test do
    assert_match version.to_s, shell_output("#{bin}/ali --version 2>&1")
  end
end
