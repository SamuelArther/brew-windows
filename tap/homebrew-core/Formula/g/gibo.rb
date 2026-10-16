class Gibo < Formula
  desc "Access GitHub's .gitignore boilerplates"
  homepage "https://github.com/simonwhitaker/gibo"
  url "https://github.com/simonwhitaker/gibo/releases/download/v3.0.22/gibo_Windows_x86_64.zip"
  sha256 "ac29ea2d4cecce4aa22d290bc88b5f14185cf12beb957b9a097b3d38c279a0d5"
  license "Unlicense"

  # Official Windows x64 release binary from the project's GitHub releases.

  livecheck do
    url "https://github.com/simonwhitaker/gibo/archive/refs/tags/v3.0.22.tar.gz"
  end

  def install
    bin.install "gibo.exe"
  end

  test do
    system bin/"gibo", "update"
    assert_includes shell_output("#{bin}/gibo dump Python"), "Python.gitignore"

    assert_match version.to_s, shell_output("#{bin}/gibo version")
  end
end
