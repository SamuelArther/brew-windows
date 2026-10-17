class XdgNinja < Formula
  desc "Check your $HOME for unwanted files and directories"
  homepage "https://github.com/b3nj5m1n/xdg-ninja"
  url "https://github.com/b3nj5m1n/xdg-ninja/archive/refs/tags/v0.2.0.2.tar.gz"
  sha256 "6adfe289821b6b5e3778130e0d1fd1851398e3bce51ddeed6c73e3eddcb806c6"
  license "MIT"
  head "https://github.com/b3nj5m1n/xdg-ninja.git", branch: "main"

  bottle do
    sha256 cellar: :any, x86_64_windows: "4e8065c1224cdbb5f6b32f133ca947d48aa95d7a06f94c1b7c6026aa6bf8af41"
  end

  depends_on "glow"

  uses_from_macos "jq", since: :sequoia

  def install
    pkgshare.install "programs/"
    pkgshare.install "xdg-ninja.sh" => "xdg-ninja"
    (bin/"xdg-ninja").write_env_script(
      pkgshare/"xdg-ninja",
      XN_PROGRAMS_DIR: "${XN_PROGRAMS_DIR:-#{pkgshare}/programs}",
    )
    man1.install "man/xdg-ninja.1"
  end

  test do
    system bin/"xdg-ninja"
  end
end
