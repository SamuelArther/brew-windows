class Melody < Formula
  desc "Language that compiles to regular expressions"
  homepage "https://yoav-lavi.github.io/melody/book"
  url "https://github.com/yoav-lavi/melody/archive/refs/tags/v0.20.0.tar.gz"
  sha256 "b0dd1b0ecc1af97f09f98a9a741e0dddbf92380c9980140140ff1b4262b9a44a"
  license any_of: ["Apache-2.0", "MIT"]

  bottle do
    sha256 cellar: :any, x86_64_windows: "9f33c3c8ac53822ebdfe643c5b37030d470f0918e0092d0921c66867d9622a1d"
  end

  depends_on "rust" => :build

  deny_network_access!

  def fetch
    system "cargo", "fetch", *std_cargo_fetch_args
  end

  def install
    system "cargo", "install", *std_cargo_args(path: "crates/melody_cli")

    generate_completions_from_executable(bin/"melody", "--generate-completions")
  end

  test do
    mdy = "regex.mdy"
    File.write mdy, '"#"; some of <word>;'
    assert_match "#\\w+", shell_output("#{bin}/melody --no-color #{mdy}")
  end
end
