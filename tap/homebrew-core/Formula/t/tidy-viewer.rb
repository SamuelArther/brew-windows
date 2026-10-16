class TidyViewer < Formula
  desc "CLI csv pretty printer"
  homepage "https://github.com/alexhallam/tv"
  url "https://github.com/alexhallam/tv/releases/download/1.8.93/tidy-viewer-1.8.93-x86_64-pc-windows-msvc.zip"
  sha256 "afaad9e7bb169b1e4439f4521506614af963a1ed22bdae439ab08f3e7d0e3030"
  license "Unlicense"

  # Official Windows x64 release binary from the project's GitHub releases.

  livecheck do
    url "https://github.com/alexhallam/tv/archive/refs/tags/1.8.93.tar.gz"
    strategy :github_latest
  end

  conflicts_with "television", because: "both install `tv` binaries"

  def install
    bin.install "tidy-viewer.exe"
  end

  test do
    assert_match version.to_s, shell_output("#{bin}/tidy-viewer --version 2>&1")
  end
end
