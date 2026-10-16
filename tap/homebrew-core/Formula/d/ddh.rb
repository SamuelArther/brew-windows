class Ddh < Formula
  desc "Fast duplicate file finder"
  homepage "https://github.com/darakian/ddh"
  url "https://github.com/darakian/ddh/releases/download/0.13.0/ddh-0.13.0-x86_64-win.exe"
  sha256 "10e74ebb676ce027220b2dcbb25f001df6d36bd9fadacb4a4d38d1c7a81a8fc6"
  license "LGPL-3.0-only"

  # Official Windows x64 release binary from the project's GitHub releases.

  livecheck do
    url "https://github.com/darakian/ddh/archive/refs/tags/0.13.0.tar.gz"
    regex(/^v?(\d+(?:\.\d+)+)$/i)
  end

  def install
    bin.install "ddh-0.13.0-x86_64-win.exe" => "ddh.exe"
  end

  test do
    (testpath/"test/file1").write "brew test"
    (testpath/"test/file2").write "brew test"

    expected = <<~EOS
      2 Total files (with duplicates): 0 Kilobytes
      1 Total files (without duplicates): 0 Kilobytes
      0 Single instance files: 0 Kilobytes
      1 Shared instance files: 0 Kilobytes (2 instances)
      Standard results written to Results.txt
    EOS

    assert_equal expected, shell_output("#{bin}/ddh -d test")

    assert_match "Duplicates", (testpath/"Results.txt").read
  end
end
