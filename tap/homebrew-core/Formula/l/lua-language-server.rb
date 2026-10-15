class LuaLanguageServer < Formula
  desc "Language Server for the Lua language"
  homepage "https://luals.github.io"
  url "https://github.com/LuaLS/lua-language-server/releases/download/3.19.2/lua-language-server-3.19.2-win32-x64.zip"
  sha256 "4a3dc851b3b31e7b75a23f645d1ba4a2137579a55f5fa4edbb17a3e2a6254044"
  license "MIT"

  livecheck do
    url :stable
    strategy :github_latest
  end

  def install
    install_tree
    windows_exe "libexec/bin/lua-language-server.exe", "lua-language-server"
  end

  test do
    assert_match version.to_s, shell_output("#{bin}/lua-language-server --version")
  end
end
