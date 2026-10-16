class Serpl < Formula
  desc "Simple terminal UI for search and replace"
  homepage "https://github.com/yassinebridi/serpl"
  url "https://github.com/yassinebridi/serpl/releases/download/0.3.11/serpl-0.3.11-windows-x86_64.tar.gz"
  sha256 "1d46b8f5a7dfeab72f012286b61c0051d03aa835ce694f9f19a423e98c0ae230"
  license "MIT"

  # Official Windows x64 release binary from the project's GitHub releases.

  livecheck do
    url "https://github.com/yassinebridi/serpl/archive/refs/tags/0.3.11.tar.gz"
  end

  def install
    bin.install "serpl.exe"
  end

  test do
    assert_path_exists bin/"serpl.exe"
  end
end
