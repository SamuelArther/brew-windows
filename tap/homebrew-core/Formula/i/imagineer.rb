class Imagineer < Formula
  desc "Image processing and conversion from the terminal"
  homepage "https://github.com/foresterre/imagineer"
  url "https://github.com/foresterre/imagineer/archive/refs/tags/v0.24.0.tar.gz"
  sha256 "2511068e1919ee67af1fb9203a1bbcfc9f24894cd3b2e341c52d493e79535e93"
  license any_of: ["Apache-2.0", "MIT"]
  head "https://github.com/foresterre/imagineer.git", branch: "main"

  bottle do
    sha256 cellar: :any, x86_64_windows: "c89ad44dcd7c6dfdb6596eb55405100c1fd4b00d78a66ccec8d18c34b77852d1"
  end

  depends_on "rust" => :build

  on_intel do
    depends_on "nasm" => :build
  end

  deny_network_access!

  def fetch
    system "cargo", "fetch", *std_cargo_fetch_args
  end

  def install
    system "cargo", "install", *std_cargo_args
  end

  test do
    assert_match version.to_s, shell_output("#{bin}/ig --version")

    system bin/"ig", "--input", test_fixtures("test.png"), "--output", "out.jpg"
    assert_path_exists testpath/"out.jpg"
  end
end
