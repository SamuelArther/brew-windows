class Moarvm < Formula
  desc "VM with adaptive optimization and JIT compilation, built for Rakudo"
  homepage "https://moarvm.org"
  url "https://github.com/MoarVM/MoarVM/releases/download/2026.09/MoarVM.windows-2022.tar.gz"
  sha256 "29ebacfddb9c7a2c3751cfbcdb15bb9be29ddb52e2b8f8410a4858a9da1fcd1d"
  license "Artistic-2.0"

  # Official Windows x64 release binary from the project's GitHub releases.

  livecheck do
    url "https://github.com/MoarVM/MoarVM/releases/download/2026.09/MoarVM-2026.09.tar.gz"
    regex(/^v?(\d+(?:\.\d+)+)$/i)
  end

  conflicts_with "moor", because: "both install `moar` binaries"

  def install
    install_tree
    windows_exe "libexec/bin/moar.exe", "moar"
  end

  test do
    assert_match version.to_s, shell_output("#{bin}/moar --version 2>&1")
  end
end
