class PfetchRs < Formula
  desc "Pretty system information tool written in Rust"
  homepage "https://github.com/Gobidev/pfetch-rs"
  url "https://github.com/Gobidev/pfetch-rs/releases/download/v3.0.2/pfetch-windows-x86_64.exe"
  sha256 "f98125b0f55ebd7bd448abf635c6519063c14e4fbd4ef14a3994af98bf86be63"
  license "MIT"

  # Official Windows x64 release binary from the project's GitHub releases.

  livecheck do
    url "https://github.com/Gobidev/pfetch-rs/archive/refs/tags/v3.0.2.tar.gz"
  end

  def install
    bin.install "pfetch-windows-x86_64.exe" => "pfetch.exe"
  end

  test do
    assert_match "uptime", shell_output("#{bin}/pfetch")
  end
end
