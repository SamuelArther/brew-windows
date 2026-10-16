class Tendermint < Formula
  desc "BFT state machine replication for applications in any programming languages"
  homepage "https://tendermint.com/"
  url "https://github.com/tendermint/tendermint/releases/download/v0.34.24/tendermint_0.34.24_windows_amd64.tar.gz"
  sha256 "7fd9161c6f75daddd97b07aca17a0ae357291e74027f0fb710f7948f22c322f6"
  license "Apache-2.0"

  # Official Windows x64 release binary from the project's GitHub releases.

  livecheck do
    url "https://github.com/tendermint/tendermint/archive/refs/tags/v0.35.9.tar.gz"
    regex(/^v?(\d+(?:\.\d+)+)$/i)
  end

  def install
    bin.install "tendermint.exe"
  end

  test do
    mkdir(testpath/"staging")
    shell_output("#{bin}/tendermint init full --home #{testpath}/staging")
    assert_path_exists testpath/"staging/config/genesis.json"
    assert_path_exists testpath/"staging/config/config.toml"
    assert_path_exists testpath/"staging/data"
  end
end
