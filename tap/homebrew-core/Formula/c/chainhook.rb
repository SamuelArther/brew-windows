class Chainhook < Formula
  desc "Reorg-aware indexing engine for the Stacks & Bitcoin blockchains"
  homepage "https://github.com/hirosystems/chainhook"
  url "https://github.com/hirosystems/chainhook/archive/refs/tags/v1.9.0.tar.gz"
  sha256 "fea917fcd18032a280a965bd84b57894008110ec15191f4efca6aaab26011443"
  license "GPL-3.0-only"
  head "https://github.com/hirosystems/chainhook.git", branch: "develop"

  livecheck do
    url :stable
    regex(/^v?(\d+(?:\.\d+)+)$/i)
  end

  bottle do
    sha256 cellar: :any, x86_64_windows: "06755bd9c2c83a624edeea18c6d1251c692bbd25185b3a0b00169284ff76d01e"
  end

  deprecate! date: "2026-02-27", because: :repo_archived
  disable! date: "2027-02-27", because: :repo_archived

  depends_on "rust" => :build

  uses_from_macos "llvm" => :build # for libclang

  def install
    features = ["cli", "debug"]
    system "cargo", "install", "--no-default-features", *std_cargo_args(path: "components/chainhook-cli", features:)
  end

  test do
    pipe_output("#{bin}/chainhook config new --mainnet", "n\n")
    assert_match "mode = \"mainnet\"", (testpath/"Chainhook.toml").read
  end
end
