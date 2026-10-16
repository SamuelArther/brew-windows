class Invoice < Formula
  desc "Command-line invoice generator"
  homepage "https://github.com/maaslalani/invoice"
  url "https://github.com/maaslalani/invoice/releases/download/v0.1.0/invoice_0.1.0_windows_amd64.tar.gz"
  sha256 "fc3aef9b5a9c120f027267a369ec0ecf50b83b58208c691961a42b5cbca1cdca"
  license "MIT"

  # Official Windows x64 release binary from the project's GitHub releases.

  livecheck do
    url "https://github.com/maaslalani/invoice/archive/refs/tags/v0.1.0.tar.gz"
  end

  def install
    bin.install "invoice.exe"
  end

  test do
    cmd = "#{bin}/invoice generate --from \"Dream, Inc.\" --to \"Imagine, Inc.\" " \
          "--item \"Rubber Duck\" --quantity 2 --rate 25 " \
          "--tax 0.13 --discount 0.15 " \
          "--note \"For debugging purposes.\""
    assert_equal "Generated invoice.pdf", shell_output(cmd).chomp
    assert_path_exists testpath/"invoice.pdf"
  end
end
