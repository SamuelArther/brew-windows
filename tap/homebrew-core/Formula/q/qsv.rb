class Qsv < Formula
  desc "Ultra-fast CSV data-wrangling toolkit"
  homepage "https://qsv.dathere.com/"
  url "https://github.com/dathere/qsv/releases/download/25.0.0/qsv-25.0.0-x86_64-pc-windows-msvc.zip"
  sha256 "d16089ec202eb759266f5a15b1cd2c765aa2323110d1fda1219e841145c7edbb"
  license any_of: ["MIT", "Unlicense"]

  livecheck do
    url :stable
    strategy :github_latest
  end

  def install
    bin.install "qsv.exe"
  end

  test do
    (testpath/"test.csv").write("first header,second header")
    assert_match "first header", shell_output("#{bin}/qsv stats #{testpath}/test.csv")
  end
end
