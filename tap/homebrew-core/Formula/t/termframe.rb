class Termframe < Formula
  desc "Terminal output SVG screenshot tool"
  homepage "https://github.com/pamburus/termframe"
  url "https://github.com/pamburus/termframe/archive/refs/tags/v0.10.1.tar.gz"
  sha256 "e0bec3693e73c5938150a32e97fb3009b7e40cfb796df3709ab8ff3770340c8b"
  license "MIT"
  head "https://github.com/pamburus/termframe.git", branch: "main"

  bottle do
    sha256 cellar: :any, x86_64_windows: "91ff0bc7afa046351d78fb7e1c4c32e410ff698b175f7325fbfaa57bb5b781cd"
  end

  depends_on "rust" => :build

  on_linux do
    depends_on "zlib-ng-compat"
  end

  # `test do` block downloads fonts.
  allow_network_access! :test

  def fetch
    system "cargo", "fetch", *std_cargo_fetch_args
  end

  def install
    system "cargo", "install", *std_cargo_args
  end

  test do
    system bin/"termframe", "-o", "hello.svg", "--", "echo", "Hello, World"
    assert_path_exists testpath/"hello.svg"
  end
end
