class Dra < Formula
  desc "Command-line tool to download release assets from GitHub"
  homepage "https://github.com/devmatteini/dra"
  url "https://github.com/devmatteini/dra/releases/download/0.10.3/dra-0.10.3-x86_64-pc-windows-msvc.zip"
  sha256 "396c44d18a92fe5584e42417f218a312cf6df856a9d966c162466c85de4b1359"
  license "MIT"

  # Official Windows x64 release binary from the project's GitHub releases.

  livecheck do
    url "https://github.com/devmatteini/dra/archive/refs/tags/0.10.3.tar.gz"
  end

  def install
    bin.install "dra.exe"
  end

  test do
    assert_match version.to_s, shell_output("#{bin}/dra --version")

    system bin/"dra", "download", "--select",
           "helloworld.tar.gz", "devmatteini/dra-tests"

    assert_path_exists testpath/"helloworld.tar.gz"
  end
end
