class Hq < Formula
  desc "Jq, but for HTML"
  homepage "https://orf.github.io/html-query/"
  url "https://github.com/orf/html-query/archive/refs/tags/html-query-v1.2.2.tar.gz"
  sha256 "0fdc12100c178cd2e5ae61c54e640ecb68533017fcee4845ceb4050d1e4fff60"
  license "MIT"

  livecheck do
    url :stable
    regex(/^html-query[._-]v?(\d+(?:\.\d+)+)$/i)
  end

  bottle do
    sha256 cellar: :any, x86_64_windows: "e2f50c3c74b9514567b8d02348ca726999abea4d61e2781d5eb49f6679fe0047"
  end

  depends_on "rust" => :build

  conflicts_with "proxygen", because: "both install `hq` binaries"

  deny_network_access!

  def fetch
    system "cargo", "fetch", *std_cargo_fetch_args
  end

  def install
    system "cargo", "install", *std_cargo_args(path: "html-query")
  end

  test do
    html = testpath/"test.html"
    html.write <<~HTML
      <p class="foo">Test</p>
    HTML
    output = shell_output("#{bin}/hq '{foo: .foo}' test.html")
    assert_match '{"foo":"Test"}', output
  end
end
