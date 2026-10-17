class N < Formula
  desc "Node version management"
  homepage "https://github.com/tj/n"
  url "https://github.com/tj/n/archive/refs/tags/v10.2.0.tar.gz"
  sha256 "5914f0d5e89aadaaaeb803baa89a7582747b0c57ad30201b3522cd76f504c7d9"
  license "MIT"
  head "https://github.com/tj/n.git", branch: "master"

  bottle do
    root_url "https://github.com/SamuelArther/brew-windows/releases/download/n-10.2.0"
    sha256 cellar: :any, x86_64_windows: "3671a1b555e2e1f475bba00dba7560a9b6e6b91fed749b28cab73504711a8154"
  end

  def install
    bin.mkdir
    system "make", "PREFIX=#{prefix}", "install"
  end

  test do
    system bin/"n", "ls"
  end
end
