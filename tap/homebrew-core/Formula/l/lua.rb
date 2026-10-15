class Lua < Formula
  desc "Powerful, lightweight programming language"
  homepage "https://www.lua.org/"
  version "5.5.0"
  url "https://downloads.sourceforge.net/project/luabinaries/5.5.0/Tools%20Executables/lua-5.5.0_Win64_bin.zip"
  sha256 "7825cb261d0dc61cb0e1511d451ceaf10bf72d2bba1855bfd3350add190e0024"
  license "MIT"

  # lua.org publishes source only. These are LuaBinaries, the Windows builds made by the Lua
  # team's home (Tecgraf/PUC-Rio) and linked from lua.org's download page. LuaBinaries has
  # no 5.5.1 build yet, so this is 5.5.0 (homebrew-core: 5.5.1).

  livecheck do
    url "https://sourceforge.net/projects/luabinaries/rss"
    regex(%r{/v?(\d+(?:\.\d+)+)/Tools(?:%20|\s)Executables/}i)
  end

  def install
    install_tree
    windows_exe "libexec/lua55.exe", "lua"
    windows_exe "libexec/luac55.exe", "luac"
    windows_exe "libexec/lua55.exe", "lua5.5"
    windows_exe "libexec/luac55.exe", "luac5.5"
  end

  test do
    assert_match "Homebrew is awesome!", shell_output("#{bin}/lua -e \"print ('Homebrew is awesome!')\"")
  end
end
