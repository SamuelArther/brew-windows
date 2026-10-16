class FuegoFirestore < Formula
  desc "Command-line client for the Firestore database"
  homepage "https://github.com/sgarciac/fuego"
  url "https://github.com/sgarciac/fuego/archive/refs/tags/0.35.0.tar.gz"
  sha256 "25446224f1d20d2e843127639450526fcdaa8e3ce03701f4ed9007821cb2020a"
  license "GPL-3.0-only"
  head "https://github.com/sgarciac/fuego.git", branch: "develop"

  bottle do
    sha256 cellar: :any_skip_relocation, x86_64_windows: "c5fd3bdb51da0798baf0b217425e485bca1016af5b391ba7fb3900e1d7b092da"
  end

  depends_on "go" => :build

  conflicts_with "fuego", because: "both install `fuego` binaries"

  deny_network_access!

  def fetch
    system "go", "mod", "download"
  end

  def install
    system "go", "build", *std_go_args(output: bin/"fuego")
  end

  test do
    collections_output = shell_output("#{bin}/fuego collections 2>&1", 80)
    assert_match "Failed to create client.", collections_output
  end
end
