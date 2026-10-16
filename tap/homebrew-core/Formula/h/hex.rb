class Hex < Formula
  desc "Futuristic take on hexdump"
  homepage "https://github.com/sitkevij/hex"
  url "https://github.com/sitkevij/hex/archive/refs/tags/v0.7.1.tar.gz"
  sha256 "32a3c45897480226fa21eafed2aab3ad4601aa21707bde314a4301fd1370b90e"
  license "MIT"
  head "https://github.com/sitkevij/hex.git", branch: "master"

  bottle do
    sha256 cellar: :any, x86_64_windows: "f066fa832e42f02b1208c041f39f68b1eefbfda43ed3542afd245e15da9c36b8"
  end

  depends_on "rust" => :build

  conflicts_with "evil-helix", because: "both install `hx` binaries"
  conflicts_with "helix", because: "both install `hx` binaries"

  deny_network_access!

  def fetch
    system "cargo", "fetch", *std_cargo_fetch_args
  end

  def install
    system "cargo", "install", *std_cargo_args
  end

  test do
    (testpath/"tiny.txt").write("il")
    output = shell_output("#{bin}/hx tiny.txt")
    assert_match "0x000000: 0x69 0x6c", output

    output = shell_output("#{bin}/hx -ar -c8 tiny.txt")
    expected = <<~EOS
      let ARRAY: [u8; 2] = [
          0x69, 0x6c
      ];
    EOS
    assert_equal expected, output

    assert_match "hx #{version}", shell_output("#{bin}/hx --version")
  end
end
