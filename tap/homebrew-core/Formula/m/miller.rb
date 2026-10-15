class Miller < Formula
  desc "Like sed, awk, cut, join & sort for name-indexed data such as CSV"
  homepage "https://github.com/johnkerl/miller"
  url "https://github.com/johnkerl/miller/releases/download/v6.22.0/miller-6.22.0-windows-amd64.zip"
  sha256 "a8b7f5fe321528d181c771948f631115b9c17a8ebdf1c5809de835349a9dd304"
  license "BSD-2-Clause"

  livecheck do
    url :stable
    strategy :github_latest
  end

  def install
    bin.install "mlr.exe"
  end

  test do
    assert_match version.to_s, shell_output("#{bin}/mlr --version")
  end
end
