class Ares < Formula
  desc "Automated decoding of encrypted text"
  homepage "https://github.com/bee-san/Ares"
  url "https://github.com/bee-san/Ares/archive/refs/tags/0.11.0.tar.gz"
  sha256 "fd8751de6c46eb523d62d4ca52018b9127b9fa5fbd4a372b7f22e0f9957f030f"
  license "MIT"

  livecheck do
    url :stable
    regex(/^v?(\d+(?:\.\d+)+)$/i)
  end

  bottle do
    sha256 cellar: :any, x86_64_windows: "33f18595e32dc9907b5bc0ab6784d34bb2393e6bccd26f70337380e49e60ecf8"
  end

  depends_on "rust" => :build

  deny_network_access!

  def fetch
    system "cargo", "fetch", *std_cargo_fetch_args
  end

  def install
    system "cargo", "install", *std_cargo_args
  end

  test do
    # base64 encoded string for "Hello from Homebrew"
    input_string = "SGVsbG8gZnJvbSBIb21lYnJldw=="
    expected_text = "Hello from Homebrew"
    # Disable custom color scheme
    output = pipe_output("#{bin}/ares -d -t #{input_string}", "N", 0)
    assert_match expected_text, output
  end
end
