class Termbg < Formula
  desc "Rust library for terminal background color detection"
  homepage "https://github.com/dalance/termbg"
  url "https://static.crates.io/crates/termbg/termbg-0.6.2.crate"
  sha256 "8bf44577a1adf3dfd7fec3b8763074467e27b2ad35ff9157bc3f0a51bb0a3dd4"
  license any_of: ["Apache-2.0", "MIT"]
  head "https://github.com/dalance/termbg.git", branch: "master"

  bottle do
    sha256 cellar: :any, x86_64_windows: "64301980821a103cc77ed76cc694a0696bb129cd8d754f576ddd0dc01ba9839c"
  end

  depends_on "rust" => :build

  deny_network_access!

  def fetch
    system "cargo", "generate-lockfile" if build.head?
    system "cargo", "fetch", *std_cargo_fetch_args
  end

  def install
    system "cargo", "install", *std_cargo_args
  end

  test do
    assert_match version.to_s, shell_output("#{bin}/termbg --version")
  end
end
