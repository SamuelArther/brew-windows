class Msdfgen < Formula
  desc "Multi-channel signed distance field generator"
  homepage "https://github.com/Chlumsky/msdfgen"
  url "https://github.com/Chlumsky/msdfgen/releases/download/v1.13/msdfgen-1.13-win64-openmp.zip"
  sha256 "e2733f999f5f93ed9e4f22fad1207917513034f43bdd5f95fb393e282096d27c"
  license "MIT"

  # Official Windows x64 release binary from the project's GitHub releases.

  livecheck do
    url "https://github.com/Chlumsky/msdfgen/archive/refs/tags/v1.13.tar.gz"
    regex(/^v?(\d+(?:\.\d+)+)$/i)
  end

  def install
    install_tree
    windows_exe "libexec/msdfgen.exe", "msdfgen"
  end

  test do
    system bin/"msdfgen", "msdf",
           "-defineshape", "{ -1, -1; m; -1, +1; y; +1, +1; m; +1, -1; y; # }",
           "-o", "out.png", "-size", "32", "32"
    assert_path_exists testpath/"out.png"
  end
end
