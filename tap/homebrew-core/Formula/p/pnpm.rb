class Pnpm < Formula
  desc "Fast, disk space efficient package manager"
  homepage "https://pnpm.io/"
  version "12.11.2"
  url "https://github.com/pnpm/pnpm/releases/download/v12.11.2/pnpm-win32-x64.zip"
  sha256 "66cac6b725bfc75fadac959fc435821048558adb50b89c82d0d2d5e57465a1dd"
  license "MIT"

  livecheck do
    url :stable
    strategy :github_latest
  end

  # Official standalone pnpm executable (bundles its own Node.js runtime).

  def install
    install_tree
    windows_exe "libexec/pnpm.exe", "pnpm"
    windows_exe "libexec/pnpm.exe", "pn"
    windows_exe "libexec/pnpm.exe", "pnpx", "dlx"
    windows_exe "libexec/pnpm.exe", "pnx", "dlx"
  end

  test do
    system bin/"pnpm", "init"
    assert_path_exists testpath/"package.json", "package.json must exist"
    assert_match version.to_s, shell_output("#{bin}/pnpm --version")
  end
end
