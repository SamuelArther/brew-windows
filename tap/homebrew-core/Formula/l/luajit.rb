class Luajit < Formula
  desc "Just-In-Time Compiler (JIT) for the Lua programming language"
  homepage "https://luajit.org/luajit.html"
  url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-luajit-2.1.1788460057-2-any.pkg.tar.zst"
  sha256 "525fab678224daf93fc88f0e346fefd56e44408a75d523a6178d0e7311fa3984"
  license "MIT"

  # Prebuilt UCRT64 package from MSYS2; its runtime packages are pinned as resources.

  livecheck do
    url "https://api.github.com/repos/LuaJIT/LuaJIT/branches/v2.1"
    strategy :json do |json|
      date = json.dig("commit", "commit", "author", "date")
      "2.1.#{DateTime.parse(date).strftime("%s")}"
    end
  end

  def install
    msys2_install
    msys2_bin "ucrt64/bin/luajit.exe", "luajit"
  end

  test do
    assert_match version.to_s, shell_output("#{bin}/luajit -v 2>&1")
  end
end
