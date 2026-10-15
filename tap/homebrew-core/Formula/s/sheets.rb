class Sheets < Formula
  desc "Terminal based spreadsheet tool"
  homepage "https://github.com/maaslalani/sheets"
  url "https://github.com/maaslalani/sheets/releases/download/v0.3.0/sheets_Windows_x86_64.zip"
  sha256 "4eb61b2c19d33aa556e31ee5e02baac5d92fc39afd5e8363d68e4550e69a5dd9"
  license "MIT"

  # Official Windows x64 release binary from the project's GitHub releases.

  livecheck do
    url "https://github.com/maaslalani/sheets/archive/refs/tags/v0.3.0.tar.gz"
  end

  def install
    bin.install "sheets.exe"
  end

  test do
    (testpath/"test.csv").write <<~CSV
      Name,Age,City
      Alice,30,NYC
      Bob,25,LA
    CSV

    assert_equal "30", shell_output("#{bin}/sheets #{testpath}/test.csv B2").strip
    assert_equal "Alice\nBob", shell_output("#{bin}/sheets #{testpath}/test.csv A2:A3").strip
  end
end
