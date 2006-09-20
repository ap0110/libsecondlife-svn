//
// socket_ops.hpp
// ~~~~~~~~~~~~~~
//
// Copyright (c) 2003-2005 Christopher M. Kohlhoff (chris at kohlhoff dot com)
//
// Distributed under the Boost Software License, Version 1.0. (See accompanying
// file LICENSE_1_0.txt or copy at http://www.boost.org/LICENSE_1_0.txt)
//

#ifndef BOOST_ASIO_DETAIL_SOCKET_OPS_HPP
#define BOOST_ASIO_DETAIL_SOCKET_OPS_HPP

#if defined(_MSC_VER) && (_MSC_VER >= 1200)
# pragma once
#endif // defined(_MSC_VER) && (_MSC_VER >= 1200)

#include <boost/asio/detail/push_options.hpp>

#include <boost/asio/detail/push_options.hpp>
#include <boost/config.hpp>
#include <cstring>
#include <cerrno>
#include <vector>
#include <boost/detail/workaround.hpp>
#include <boost/asio/detail/pop_options.hpp>

#include <boost/asio/error.hpp>
#include <boost/asio/detail/socket_types.hpp>

namespace boost {
namespace asio {
namespace detail {
namespace socket_ops {

inline int get_error()
{
#if defined(BOOST_WINDOWS)
  return WSAGetLastError();
#else // defined(BOOST_WINDOWS)
  return errno;
#endif // defined(BOOST_WINDOWS)
}

inline void set_error(int error)
{
  errno = error;
#if defined(BOOST_WINDOWS)
  WSASetLastError(error);
#endif // defined(BOOST_WINDOWS)
}

template <typename ReturnType>
inline ReturnType error_wrapper(ReturnType return_value)
{
#if defined(BOOST_WINDOWS)
  errno = WSAGetLastError();
#endif // defined(BOOST_WINDOWS)
  return return_value;
}

inline socket_type accept(socket_type s, socket_addr_type* addr,
    socket_addr_len_type* addrlen)
{
  set_error(0);
  return error_wrapper(::accept(s, addr, addrlen));
}

inline int bind(socket_type s, const socket_addr_type* addr,
    socket_addr_len_type addrlen)
{
  set_error(0);
  return error_wrapper(::bind(s, addr, addrlen));
}

inline int close(socket_type s)
{
  set_error(0);
#if defined(BOOST_WINDOWS)
  return error_wrapper(::closesocket(s));
#else // defined(BOOST_WINDOWS)
  return error_wrapper(::close(s));
#endif // defined(BOOST_WINDOWS)
}

inline int shutdown(socket_type s, int what)
{
  set_error(0);
  return error_wrapper(::shutdown(s, what));
}

inline int connect(socket_type s, const socket_addr_type* addr,
    socket_addr_len_type addrlen)
{
  set_error(0);
  return error_wrapper(::connect(s, addr, addrlen));
}

inline int listen(socket_type s, int backlog)
{
  set_error(0);
  return error_wrapper(::listen(s, backlog));
}

struct bufs
{
  void* data;
  size_t size;
};

enum { max_bufs = 16 };

inline int recv(socket_type s, bufs* b, size_t count, int flags)
{
  set_error(0);
#if defined(BOOST_WINDOWS)
  // Copy buffers into WSABUF array.
  WSABUF recv_bufs[max_bufs];
  if (count > max_bufs)
    count = max_bufs;
  for (size_t i = 0; i < count; ++i)
  {
    recv_bufs[i].len = static_cast<u_long>(b[i].size);
    recv_bufs[i].buf = static_cast<char*>(b[i].data);
  }

  // Receive some data.
  DWORD recv_buf_count = static_cast<DWORD>(count);
  DWORD bytes_transferred = 0;
  DWORD recv_flags = flags;
  int result = error_wrapper(::WSARecv(s, recv_bufs,
        recv_buf_count, &bytes_transferred, &recv_flags, 0, 0));
  if (result != 0)
    return -1;
  return bytes_transferred;
#else // defined(BOOST_WINDOWS)
  if (count == 1)
  {
    // We only have to receive into a single buffer, so use normal recv call.
    return error_wrapper(::recv(s, b[0].data, b[0].size, flags));
  }
  else if (flags == 0)
  {
    // No flags have been specified so we can perform this receive operation
    // using a vectored-read function, i.e. readv.

    // Copy buffers into iovec array.
    iovec recv_bufs[max_bufs];
    if (count > max_bufs)
      count = max_bufs;
    for (size_t i = 0; i < count; ++i)
    {
      recv_bufs[i].iov_len = b[i].size;
      recv_bufs[i].iov_base = b[i].data;
    }

    // Receive some data.
    return error_wrapper(::readv(s, recv_bufs, count));
  }
  else
  {
    // Socket flags have been supplied so receive into a temporary buffer and
    // then copy the data to the caller-supplied buffers.

    // Create a buffer of the appropriate size.
    size_t buf_size = 0;
    for (size_t i = 0; i < count; ++i)
      buf_size += b[i].size;
    std::vector<char> buffer(buf_size);

    // Receive some data.
    int result = error_wrapper(::recv(s, &buffer[0], buf_size, flags));
    if (result <= 0)
      return result;

    // Copy to caller-supplied buffers.
    using namespace std; // For memcpy.
    size_t bytes_avail = result;
    size_t bytes_copied = 0;
    for (size_t i = 0; i < count && bytes_avail > 0; ++i)
    {
      size_t size = (b[i].size < bytes_avail) ? b[i].size : bytes_avail;
      memcpy(b[i].data, &buffer[bytes_copied], size);
      bytes_copied += size;
      bytes_avail -= size;
    }

    return result;
  }
#endif // defined(BOOST_WINDOWS)
}

inline int recvfrom(socket_type s, bufs* b, size_t count, int flags,
    socket_addr_type* addr, socket_addr_len_type* addrlen)
{
  set_error(0);
#if defined(BOOST_WINDOWS)
  // Copy buffers into WSABUF array.
  WSABUF recv_bufs[max_bufs];
  if (count > max_bufs)
    count = max_bufs;
  for (size_t i = 0; i < count; ++i)
  {
    recv_bufs[i].len = static_cast<u_long>(b[i].size);
    recv_bufs[i].buf = static_cast<char*>(b[i].data);
  }

  // Receive some data.
  DWORD recv_buf_count = static_cast<DWORD>(count);
  DWORD bytes_transferred = 0;
  DWORD recv_flags = flags;
  int result = error_wrapper(::WSARecvFrom(s, recv_bufs, recv_buf_count,
        &bytes_transferred, &recv_flags, addr, addrlen, 0, 0));
  if (result != 0)
    return -1;
  return bytes_transferred;
#else // defined(BOOST_WINDOWS)
  if (count == 1)
  {
    // We only have to receive into a single buffer, so use normal recvfrom
    // call.
    return error_wrapper(::recvfrom(s,
          b[0].data, b[0].size, flags, addr, addrlen));
  }
  else
  {
    // We have to receive into multiple buffers, so receive into a temporary
    // buffer and then copy the data to the caller-supplied buffers.

    // Create a buffer of the appropriate size.
    size_t buf_size = 0;
    for (size_t i = 0; i < count; ++i)
      buf_size += b[i].size;
    std::vector<char> buffer(buf_size);

    // Receive some data.
    int result = error_wrapper(::recvfrom(s,
          &buffer[0], buf_size, flags, addr, addrlen));
    if (result <= 0)
      return result;

    // Copy to caller-supplied buffers.
    using namespace std; // For memcpy.
    size_t bytes_avail = result;
    size_t bytes_copied = 0;
    for (size_t i = 0; i < count && bytes_avail > 0; ++i)
    {
      size_t size = (b[i].size < bytes_avail) ? b[i].size : bytes_avail;
      memcpy(b[i].data, &buffer[bytes_copied], size);
      bytes_copied += size;
      bytes_avail -= size;
    }

    return result;
  }
#endif // defined(BOOST_WINDOWS)
}

inline int send(socket_type s, const bufs* b, size_t count, int flags)
{
  set_error(0);
#if defined(BOOST_WINDOWS)
  // Copy buffers into WSABUF array.
  WSABUF send_bufs[max_bufs];
  if (count > max_bufs)
    count = max_bufs;
  for (size_t i = 0; i < count; ++i)
  {
    send_bufs[i].len = static_cast<u_long>(b[i].size);
    send_bufs[i].buf = static_cast<char*>(b[i].data);
  }

  // Send the data.
  DWORD send_buf_count = static_cast<DWORD>(count);
  DWORD bytes_transferred = 0;
  DWORD send_flags = flags;
  int result = error_wrapper(::WSASend(s, send_bufs,
        send_buf_count, &bytes_transferred, send_flags, 0, 0));
  if (result != 0)
    return -1;
  return bytes_transferred;
#else // defined(BOOST_WINDOWS)
  if (count == 1)
  {
    // We only have to send a single buffer, so use normal send call.
    return error_wrapper(::send(s, b[0].data, b[0].size, flags));
  }
  else if (flags == 0)
  {
    // No flags have been specified so we can perform this send operation using
    // a vectored-write function, i.e. writev.

    // Copy buffers into iovec array.
    iovec send_bufs[max_bufs];
    if (count > max_bufs)
      count = max_bufs;
    for (size_t i = 0; i < count; ++i)
    {
      send_bufs[i].iov_len = b[i].size;
      send_bufs[i].iov_base = b[i].data;
    }

    // Send the data.
    return error_wrapper(::writev(s, send_bufs, count));
  }
  else
  {
    // Socket flags have been supplied so copy the caller-supplied buffers into
    // a temporary buffer for sending.

    // Create a buffer of the appropriate size.
    size_t buf_size = 0;
    for (size_t i = 0; i < count; ++i)
      buf_size += b[i].size;
    std::vector<char> buffer(buf_size);

    // Copy data from caller-supplied buffers.
    using namespace std; // For memcpy.
    size_t bytes_copied = 0;
    for (size_t i = 0; i < count; ++i)
    {
      memcpy(&buffer[bytes_copied], b[i].data, b[i].size);
      bytes_copied += b[i].size;
    }

    // Receive some data.
    return error_wrapper(::send(s, &buffer[0], buf_size, flags));
  }
#endif // defined(BOOST_WINDOWS)
}

inline int sendto(socket_type s, const bufs* b, size_t count, int flags,
    const socket_addr_type* addr, socket_addr_len_type addrlen)
{
  set_error(0);
#if defined(BOOST_WINDOWS)
  // Copy buffers into WSABUF array.
  WSABUF send_bufs[max_bufs];
  if (count > max_bufs)
    count = max_bufs;
  for (size_t i = 0; i < count; ++i)
  {
    send_bufs[i].len = static_cast<u_long>(b[i].size);
    send_bufs[i].buf = static_cast<char*>(b[i].data);
  }

  // Send the data.
  DWORD send_buf_count = static_cast<DWORD>(count);
  DWORD bytes_transferred = 0;
  int result = ::WSASendTo(s, send_bufs, send_buf_count,
      &bytes_transferred, flags, addr, addrlen, 0, 0);
  if (result != 0)
    return -1;
  return bytes_transferred;
#else // defined(BOOST_WINDOWS)
  if (count == 1)
  {
    // We only have to send a single buffer, so use normal sendto call.
    return error_wrapper(::sendto(s,
          b[0].data, b[0].size, flags, addr, addrlen));
  }
  else
  {
    // Socket flags have been supplied so copy the caller-supplied buffers into
    // a temporary buffer for sending.

    // Create a buffer of the appropriate size.
    size_t buf_size = 0;
    for (size_t i = 0; i < count; ++i)
      buf_size += b[i].size;
    std::vector<char> buffer(buf_size);

    // Copy data from caller-supplied buffers.
    using namespace std; // For memcpy.
    size_t bytes_copied = 0;
    for (size_t i = 0; i < count; ++i)
    {
      memcpy(&buffer[bytes_copied], b[i].data, b[i].size);
      bytes_copied += b[i].size;
    }

    // Receive some data.
    return error_wrapper(::sendto(s,
          &buffer[0], buf_size, flags, addr, addrlen));
  }
#endif // defined(BOOST_WINDOWS)
}

inline socket_type socket(int af, int type, int protocol)
{
  set_error(0);
#if defined(BOOST_WINDOWS)
  return error_wrapper(::WSASocket(af, type, protocol, 0, 0,
        WSA_FLAG_OVERLAPPED));
#else // defined(BOOST_WINDOWS)
  return error_wrapper(::socket(af, type, protocol));
#endif // defined(BOOST_WINDOWS)
}

inline int setsockopt(socket_type s, int level, int optname,
    const void* optval, size_t optlen)
{
  set_error(0);
#if defined(BOOST_WINDOWS)
  return error_wrapper(::setsockopt(s, level, optname,
        reinterpret_cast<const char*>(optval), static_cast<int>(optlen)));
#else // defined(BOOST_WINDOWS)
  return error_wrapper(::setsockopt(s, level, optname, optval,
        static_cast<socklen_t>(optlen)));
#endif // defined(BOOST_WINDOWS)
}

inline int getsockopt(socket_type s, int level, int optname, void* optval,
    size_t* optlen)
{
  set_error(0);
#if defined(BOOST_WINDOWS)
  int tmp_optlen = static_cast<int>(*optlen);
  int result = error_wrapper(::getsockopt(s, level, optname,
        reinterpret_cast<char*>(optval), &tmp_optlen));
  *optlen = static_cast<size_t>(tmp_optlen);
  return result;
#else // defined(BOOST_WINDOWS)
  socklen_t tmp_optlen = static_cast<socklen_t>(*optlen);
  int result = error_wrapper(::getsockopt(s, level, optname,
        optval, &tmp_optlen));
  *optlen = static_cast<size_t>(tmp_optlen);
  return result;
#endif // defined(BOOST_WINDOWS)
}

inline int getpeername(socket_type s, socket_addr_type* addr,
    socket_addr_len_type* addrlen)
{
  set_error(0);
  return error_wrapper(::getpeername(s, addr, addrlen));
}

inline int getsockname(socket_type s, socket_addr_type* addr,
    socket_addr_len_type* addrlen)
{
  set_error(0);
  return error_wrapper(::getsockname(s, addr, addrlen));
}

inline int ioctl(socket_type s, long cmd, ioctl_arg_type* arg)
{
  set_error(0);
#if defined(BOOST_WINDOWS)
  return error_wrapper(::ioctlsocket(s, cmd, arg));
#else // defined(BOOST_WINDOWS)
  return error_wrapper(::ioctl(s, cmd, arg));
#endif // defined(BOOST_WINDOWS)
}

inline int select(int nfds, fd_set* readfds, fd_set* writefds,
    fd_set* exceptfds, timeval* timeout)
{
  set_error(0);
#if defined(BOOST_WINDOWS)
  if (!readfds && !writefds && !exceptfds && timeout)
  {
    DWORD milliseconds = timeout->tv_sec * 1000 + timeout->tv_usec / 1000;
    if (milliseconds == 0)
      milliseconds = 1; // Force context switch.
    ::Sleep(milliseconds);
    return 0;
  }
#endif // defined(BOOST_WINDOWS)
  return error_wrapper(::select(nfds, readfds, writefds, exceptfds, timeout));
}

inline const char* inet_ntop(int af, const void* src, char* dest,
    size_t length)
{
  set_error(0);
#if defined(BOOST_WINDOWS)
  using namespace std; // For strncat.

  if (af != AF_INET)
  {
    set_error(boost::asio::error::address_family_not_supported);
    return 0;
  }

  char* addr_str = error_wrapper(
      ::inet_ntoa(*static_cast<const in_addr*>(src)));
  if (addr_str)
  {
    *dest = '\0';
#if BOOST_WORKAROUND(BOOST_MSVC, >= 1400)
    strncat_s(dest, length, addr_str, length);
#else
    strncat(dest, addr_str, length);
#endif
    return dest;
  }

  // Windows may not set an error code on failure.
  if (get_error() == 0)
    set_error(boost::asio::error::invalid_argument);

  return 0;

#else // defined(BOOST_WINDOWS)
  const char* result = error_wrapper(::inet_ntop(af, src, dest, length));
  if (result == 0 && get_error() == 0)
    set_error(boost::asio::error::invalid_argument);
  return result;
#endif // defined(BOOST_WINDOWS)
}

inline int inet_pton(int af, const char* src, void* dest)
{
  set_error(0);
#if defined(BOOST_WINDOWS)
  using namespace std; // For strcmp.

  if (af != AF_INET)
  {
    set_error(boost::asio::error::address_family_not_supported);
    return -1;
  }

  u_long_type addr = error_wrapper(::inet_addr(src));
  if (addr != INADDR_NONE || strcmp(src, "255.255.255.255") == 0)
  {
    static_cast<in_addr*>(dest)->s_addr = addr;
    return 1;
  }

  // Windows may not set an error code on failure.
  if (get_error() == 0)
    set_error(boost::asio::error::invalid_argument);

  return 0;
#else // defined(BOOST_WINDOWS)
  int result = error_wrapper(::inet_pton(af, src, dest));
  if (result <= 0 && get_error() == 0)
    set_error(boost::asio::error::invalid_argument);
  return result;
#endif // defined(BOOST_WINDOWS)
}

inline int gethostname(char* name, int namelen)
{
  set_error(0);
  return error_wrapper(::gethostname(name, namelen));
}

inline int translate_netdb_error(int error)
{
  switch (error)
  {
  case 0:
    return boost::asio::error::success;
  case HOST_NOT_FOUND:
    return boost::asio::error::host_not_found;
  case TRY_AGAIN:
    return boost::asio::error::host_not_found_try_again;
  case NO_RECOVERY:
    return boost::asio::error::no_recovery;
  case NO_DATA:
    return boost::asio::error::no_host_data;
  default:
    BOOST_ASSERT(false);
    return get_error();
  }
}

inline hostent* gethostbyaddr(const char* addr, int length, int type,
    hostent* result, char* buffer, int buflength, int* error)
{
  set_error(0);
#if defined(BOOST_WINDOWS)
  hostent* retval = error_wrapper(::gethostbyaddr(addr, length, type));
  *error = get_error();
  if (!retval)
    return 0;
  *result = *retval;
  return retval;
#elif defined(__sun)
  hostent* retval = error_wrapper(::gethostbyaddr_r(addr, length, type, result,
        buffer, buflength, error));
  *error = translate_netdb_error(*error);
  return retval;
#elif defined(__MACH__) && defined(__APPLE__)
  hostent* retval = error_wrapper(::getipnodebyaddr(addr, length, type, error));
  *error = translate_netdb_error(*error);
  if (!retval)
    return 0;
  *result = *retval;
  return retval;
#else
  hostent* retval = 0;
  error_wrapper(::gethostbyaddr_r(addr, length, type, result, buffer,
        buflength, &retval, error));
  *error = translate_netdb_error(*error);
  return retval;
#endif
}

inline hostent* gethostbyname(const char* name, struct hostent* result,
    char* buffer, int buflength, int* error)
{
  set_error(0);
#if defined(BOOST_WINDOWS)
  hostent* retval = error_wrapper(::gethostbyname(name));
  *error = get_error();
  if (!retval)
    return 0;
  *result = *retval;
  return result;
#elif defined(__sun)
  hostent* retval = error_wrapper(::gethostbyname_r(name, result, buffer,
        buflength, error));
  *error = translate_netdb_error(*error);
  return retval;
#elif defined(__MACH__) && defined(__APPLE__)
  hostent* retval = error_wrapper(::getipnodebyname(name, AF_INET, 0, error));
  *error = translate_netdb_error(*error);
  if (!retval)
    return 0;
  *result = *retval;
  return retval;
#else
  hostent* retval = 0;
  error_wrapper(::gethostbyname_r(name, result, buffer, buflength, &retval,
        error));
  *error = translate_netdb_error(*error);
  return retval;
#endif
}

inline void freehostent(hostent* h)
{
#if defined(__MACH__) && defined(__APPLE__)
  ::freehostent(h);
#endif
}

inline u_long_type network_to_host_long(u_long_type value)
{
  return ntohl(value);
}

inline u_long_type host_to_network_long(u_long_type value)
{
  return htonl(value);
}

inline u_short_type network_to_host_short(u_short_type value)
{
  return ntohs(value);
}

inline u_short_type host_to_network_short(u_short_type value)
{
  return htons(value);
}

} // namespace socket_ops
} // namespace detail
} // namespace asio
} // namespace boost

#include <boost/asio/detail/pop_options.hpp>

#endif // BOOST_ASIO_DETAIL_SOCKET_OPS_HPP