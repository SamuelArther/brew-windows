class Rune < Formula
  desc "Embeddable dynamic programming language for Rust"
  homepage "https://rune-rs.github.io"
  url "https://github.com/rune-rs/rune/archive/refs/tags/0.14.2.tar.gz"
  sha256 "a858800d066f47e101c9b613d04dbc3f3d6d2bdb932da92f37c1ccdc79077337"
  license any_of: ["Apache-2.0", "MIT"]
  head "https://github.com/rune-rs/rune.git", branch: "main"

  bottle do
    sha256 cellar: :any, x86_64_windows: "74d58fade6918c2cdf0ce62688bacb995e150b608dc20da6d90f659dc324ee36"
  end

  depends_on "rust" => :build

  def install
    system "cargo", "install", *std_cargo_args(path: "crates/rune-cli")
    system "cargo", "install", *std_cargo_args(path: "crates/rune-languageserver")
  end

  test do
    (testpath/"main.rn").write <<~RN
      pub fn main() {
        println!("Hello, world!");
      }
    RN

    assert_equal "Hello, world!", shell_output("#{bin/"rune"} run").strip
  end
end
