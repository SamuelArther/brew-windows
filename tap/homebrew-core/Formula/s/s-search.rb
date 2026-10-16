class SSearch < Formula
  desc "Web search from the terminal"
  homepage "https://github.com/zquestz/s"
  url "https://github.com/zquestz/s/releases/download/v0.8.1/s-windows_amd64.zip"
  sha256 "3135ca568023e31e678a489d1e48fa99bbe8261ff13b0a51553e44250a8621a0"
  license "MIT"

  # Official Windows x64 release binary from the project's GitHub releases.

  livecheck do
    url "https://github.com/zquestz/s/archive/refs/tags/v0.8.1.tar.gz"
  end

  def install
    bin.install "s.exe"
  end

  test do
    output = shell_output("#{bin}/s -p bing -b echo homebrew")
    assert_equal "https://www.bing.com/search?q=homebrew", output.chomp
  end
end
