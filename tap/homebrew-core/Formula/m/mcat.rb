class Mcat < Formula
  desc "Terminal image, video, directory, and Markdown viewer"
  homepage "https://github.com/Skardyy/mcat"
  url "https://github.com/Skardyy/mcat/releases/download/v0.6.5/mcat-v0.6.5-x86_64-pc-windows-msvc.zip"
  sha256 "51a8bfda59f5dbc5a5b286cfbb94c5919d484ea8db00a35fab15fc863423e15c"
  license "MIT"

  # Official Windows x64 release binary from the project's GitHub releases.

  livecheck do
    url "https://github.com/Skardyy/mcat/archive/refs/tags/v0.6.5.tar.gz"
  end

  conflicts_with "mtools", because: "both install `mcat` binaries"

  def install
    bin.install "mcat.exe"
  end

  test do
    assert_match version.to_s, shell_output("#{bin}/mcat --version")

    (testpath/"test.md").write <<~MD
      # Hello World

      This is a **test** of _mcat_!
    MD

    output = shell_output("#{bin}/mcat #{testpath}/test.md")
    assert_match "# Hello World\n\nThis is a **test** of _mcat_!", output
  end
end
