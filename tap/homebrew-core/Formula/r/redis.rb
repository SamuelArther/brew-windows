class Redis < Formula
  desc "Persistent key-value database, with built-in net interface"
  homepage "https://redis.io/"
  url "https://github.com/redis-windows/redis-windows/releases/download/8.10.2/Redis-8.10.2-Windows-x64-msys2.zip"
  sha256 "7c8cebd50347eaa1d9e784da842ed47a4f33394637835531d6614777b950ee85"
  license all_of: [
    "AGPL-3.0-only", # modules: VectorSimilarity, LibMR
    "Apache-2.0", # modules: ScalableVectorSearch, cpu_features, friso, cndict
    "BSD-2-Clause", # deps/jemalloc, deps/linenoise, src/lzf*, deps/tre, deps/xxhash, modules: bloom
    "BSD-3-Clause", # modules: libevent, hiredis, readies, snowball, stemmers
    "BSL-1.0", # deps/fpconv, modules: boost, eve, dragonbox, fast_double_parser
    "MIT", # deps/lua, modules: fmt, spdlog, robin-map, tomlplusplus, fast_float, t-digest-c, libnu, libuv, miniz
    { any_of: ["CC0-1.0", "BSD-2-Clause"] }, # deps/hdr_histogram
    any_of: ["Artistic-1.0-Perl", "GPL-1.0-or-later"], # modules: phonetics
  ]
  revision 1

  livecheck do
    url :stable
    strategy :github_latest
  end

  # Community MSYS2-based Redis Windows port; not an official Redis Windows build.
  # No service is registered or started.

  def install
    install_tree
    windows_exe "libexec/redis-server.exe", "redis-server"
    windows_exe "libexec/redis-cli.exe", "redis-cli"
    windows_exe "libexec/redis-benchmark.exe", "redis-benchmark"
  end

  test do
    system bin/"redis-server", "--test-memory", "2"
    assert_match version.to_s, shell_output("#{bin}/redis-server --version")
    assert_match version.to_s, shell_output("#{bin}/redis-cli --version")
  end
end
