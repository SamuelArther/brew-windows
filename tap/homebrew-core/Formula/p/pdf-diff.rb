class PdfDiff < Formula
  desc "Tool for visualizing differences between two pdf files"
  homepage "https://github.com/serhack/pdf-diff"
  url "https://github.com/serhack/pdf-diff/releases/download/v0.0.1/pdf-diff_0.0.1_Windows_x86_64.tar.gz"
  sha256 "09d578a2539189fe4743bd84348418fd66b4b8a932f85bae9cf950b364d76c3a"
  license "MIT"

  # Official Windows x64 release binary from the project's GitHub releases.

  livecheck do
    url "https://github.com/serhack/pdf-diff/archive/refs/tags/v0.0.1.tar.gz"
  end

  def install
    bin.install "pdf-diff.exe"
  end

  test do
    system bin/"pdf-diff", "--help"
  end
end
