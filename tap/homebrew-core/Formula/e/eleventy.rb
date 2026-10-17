class Eleventy < Formula
  desc "Simpler static site generator"
  homepage "https://www.11ty.dev"
  url "https://registry.npmjs.org/@11ty/eleventy/-/eleventy-3.1.7.tgz"
  sha256 "71a54c83855a9eec0171321d9aded12de5d8635cbbf06dd8edfbc92d9cb3ef58"
  license "MIT"
  head "https://github.com/11ty/eleventy.git", branch: "main"

  bottle do
    sha256 cellar: :any, x86_64_windows: "b9803b24016ca47ec0457e3fb3b85fb0461df01b8bfb1dabe22c50d8b16bbaf7"
  end

  depends_on "node"

  def install
    system "npm", "install", *std_npm_args
    bin.install_symlink libexec.glob("bin/*")
  end

  test do
    (testpath/"README.md").write "# Hello from Homebrew\nThis is a test."
    system bin/"eleventy"
    assert_equal "<h1>Hello from Homebrew</h1>\n<p>This is a test.</p>\n",
                 (testpath/"_site/README/index.html").read
  end
end
