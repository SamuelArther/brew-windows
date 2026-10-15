class Xh < Formula
  desc "Friendly and fast tool for sending HTTP requests"
  homepage "https://github.com/ducaale/xh"
  url "https://github.com/ducaale/xh/releases/download/v0.26.2/xh-v0.26.2-x86_64-pc-windows-msvc.zip"
  sha256 "7907c1ef225382fb5955c8274aa23e31b70622c4d39dbc76cf945dc8ce15a78d"
  license "MIT"

  livecheck do
    url :stable
    strategy :github_latest
  end

  def install
    install_tree
    windows_exe "libexec/xh.exe", "xh"
    windows_exe "libexec/xh.exe", "xhs", "--https"
  end

  test do
    assert_match "xh #{version}", shell_output("#{bin}/xh --version")
  end
end
