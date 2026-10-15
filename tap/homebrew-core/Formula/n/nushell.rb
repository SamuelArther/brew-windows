class Nushell < Formula
  desc "Modern shell for the GitHub era"
  homepage "https://www.nushell.sh"
  url "https://github.com/nushell/nushell/releases/download/0.116.1/nu-0.116.1-x86_64-pc-windows-msvc.zip"
  sha256 "6658c9e61fe42c88326c1caa99e8dc9ad99bec6c0bc353e6ecb7c5d3f79feed7"
  license "MIT"

  livecheck do
    url :stable
    strategy :github_latest
  end

  def install
    install_tree
    windows_exe "libexec/nu.exe", "nu"
    windows_exe "libexec/nu_plugin_formats.exe", "nu_plugin_formats"
    windows_exe "libexec/nu_plugin_gstat.exe", "nu_plugin_gstat"
    windows_exe "libexec/nu_plugin_inc.exe", "nu_plugin_inc"
    windows_exe "libexec/nu_plugin_polars.exe", "nu_plugin_polars"
    windows_exe "libexec/nu_plugin_query.exe", "nu_plugin_query"
  end

  test do
    assert_match "homebrew_test", shell_output("#{bin}/nu -c \"echo homebrew_test\"")
  end
end
