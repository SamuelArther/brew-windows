class Sd < Formula
  desc "Intuitive find & replace CLI"
  homepage "https://github.com/chmln/sd"
  url "https://github.com/chmln/sd/releases/download/v1.1.0/sd-v1.1.0-x86_64-pc-windows-msvc.zip"
  sha256 "59837c2e7c911099aca1cc46b663bcdc5a949fd3e9fbbaf34fc73e5d5d71007c"
  license "MIT"

  livecheck do
    url :stable
    strategy :github_latest
  end

  def install
    install_tree
    windows_exe "libexec/sd.exe", "sd"
  end

  test do
    assert_equal "after", pipe_output("#{bin}/sd before after", "before").chomp
  end
end
