class GrinWallet < Formula
  desc "Official wallet for the cryptocurrency Grin"
  homepage "https://grin.mw"
  url "https://github.com/mimblewimble/grin-wallet/releases/download/v5.5.1/grin-wallet-v5.5.1-win-x86_64.zip"
  sha256 "5057f998edbfc526eb836da2192357b70e7f7508cbea6532952eca13de505b85"
  license "Apache-2.0"

  # Official Windows x64 release binary from the project's GitHub releases.

  livecheck do
    url "https://github.com/mimblewimble/grin-wallet/archive/refs/tags/v5.5.1.tar.gz"
  end

  def install
    bin.install "grin-wallet.exe"
  end

  test do
    assert_match version.to_s, shell_output("#{bin}/grin-wallet --version 2>&1")
  end
end
