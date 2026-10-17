class H2c < Formula
  desc "Headers 2 curl"
  homepage "https://curl.se/h2c/"
  url "https://github.com/curl/h2c/archive/refs/tags/1.0.tar.gz"
  sha256 "1c5e4d76131abb5151c89cc54945256509dad9d12cab36205aa5bcd7f8a311af"
  license "MIT"

  bottle do
    root_url "https://github.com/SamuelArther/brew-windows/releases/download/h2c-1.0"
    sha256 cellar: :any, x86_64_windows: "755e71a7e316dc88a55e58c08cd2028bdd755e48fc92e90b32544b6aa2b716a0"
  end

  def install
    bin.install "h2c"
  end

  test do
    assert_match "h2c.pl [options] < file", shell_output("#{bin}/h2c --help")

    # test if h2c can convert HTTP headers to curl options.
    assert_match "curl --head --http1.1 --header Accept: --header \"Shoesize: 12\" --user-agent \"moo\" https://example.com/",
      pipe_output(bin/"h2c", "HEAD  / HTTP/1.1\nHost: example.com\nUser-Agent: moo\nShoesize: 12")
  end
end
