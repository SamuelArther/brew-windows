class Termframe < Formula
  desc "Terminal output SVG screenshot tool"
  homepage "https://github.com/pamburus/termframe"
  url "https://github.com/pamburus/termframe/archive/refs/tags/v0.9.3.tar.gz"
  sha256 "6e33bfb8c4b8fc741a736b596342797d82e87d4335cc3932dffacce16c4197b8"
  license "MIT"
  head "https://github.com/pamburus/termframe.git", branch: "main"

  bottle do
    sha256 cellar: :any, x86_64_windows: "6598e45d68cc4ebd97d88cf4605d8796e80c633401e61ce8cac74414eda14f7c"
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
