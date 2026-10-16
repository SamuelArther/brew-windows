class Stormy < Formula
  desc "Minimal, customizable and neofetch-like weather CLI based on rainy"
  homepage "https://github.com/ashish0kumar/stormy"
  url "https://github.com/ashish0kumar/stormy/releases/download/v0.3.3/stormy_windows_x86_64.zip"
  sha256 "7f16d3c92d492b56ac82e507f176c5f1702fd8ba73c0c2acdb90edfae5b7d519"
  license "MIT"

  # Official Windows x64 release binary from the project's GitHub releases.

  livecheck do
    url "https://github.com/ashish0kumar/stormy/archive/refs/tags/v0.3.3.tar.gz"
  end

  def install
    bin.install "stormy.exe"
  end

  test do
    assert_match "Weather", shell_output("#{bin}/stormy --city London")
    assert_match "Error: City must be set in the config file or via command line flags",
      shell_output("#{bin}/stormy 2>&1", 1)
  end
end
