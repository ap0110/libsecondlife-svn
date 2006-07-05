//
// socket_option.hpp
// ~~~~~~~~~~~~~~~~~
//
// Copyright (c) 2003-2005 Christopher M. Kohlhoff (chris at kohlhoff dot com)
//
// Distributed under the Boost Software License, Version 1.0. (See accompanying
// file LICENSE_1_0.txt or copy at http://www.boost.org/LICENSE_1_0.txt)
//

#ifndef BOOST_ASIO_IPV4_DETAIL_SOCKET_OPTION_HPP
#define BOOST_ASIO_IPV4_DETAIL_SOCKET_OPTION_HPP

#if defined(_MSC_VER) && (_MSC_VER >= 1200)
# pragma once
#endif // defined(_MSC_VER) && (_MSC_VER >= 1200)

#include <boost/asio/detail/push_options.hpp>

#include <boost/asio/detail/push_options.hpp>
#include <cstddef>
#include <boost/config.hpp>
#include <boost/asio/detail/pop_options.hpp>

#include <boost/asio/ipv4/address.hpp>
#include <boost/asio/detail/socket_ops.hpp>
#include <boost/asio/detail/socket_types.hpp>

namespace boost {
namespace asio {
namespace ipv4 {
namespace detail {
namespace socket_option {

// Helper template for implementing address-based options.
template <int Level, int Name>
class address
{
public:
  // Default constructor.
  address()
  {
    value_.s_addr = boost::asio::detail::socket_ops::host_to_network_long(
        boost::asio::ipv4::address::any().to_ulong());
  }

  // Construct with address.
  address(const boost::asio::ipv4::address& value)
  {
    value_.s_addr =
      boost::asio::detail::socket_ops::host_to_network_long(value.to_ulong());
  }

  // Get the level of the socket option.
  int level() const
  {
    return Level;
  }

  // Get the name of the socket option.
  int name() const
  {
    return Name;
  }

  // Set the value of the socket option.
  void set(const boost::asio::ipv4::address& value)
  {
    value_.s_addr =
      boost::asio::detail::socket_ops::host_to_network_long(value.to_ulong());
  }

  // Get the current value of the socket option.
  boost::asio::ipv4::address get() const
  {
    return boost::asio::ipv4::address(
        boost::asio::detail::socket_ops::network_to_host_long(value_.s_addr));
  }

  // Get the address of the option data.
  in_addr* data()
  {
    return &value_;
  }

  // Get the address of the option data.
  const in_addr* data() const
  {
    return &value_;
  }

  // Get the size of the option data.
  std::size_t size() const
  {
    return sizeof(value_);
  }

private:
  in_addr value_;
};

// Helper template for implementing ip_mreq-based options.
template <int Level, int Name>
class multicast_request
{
public:
  // Default constructor.
  multicast_request()
  {
    value_.imr_multiaddr.s_addr =
      boost::asio::detail::socket_ops::host_to_network_long(
          boost::asio::ipv4::address::any().to_ulong());
    value_.imr_interface.s_addr =
      boost::asio::detail::socket_ops::host_to_network_long(
          boost::asio::ipv4::address::any().to_ulong());
  }

  // Construct with multicast address only.
  multicast_request(const boost::asio::ipv4::address& multicast_address)
  {
    value_.imr_multiaddr.s_addr =
      boost::asio::detail::socket_ops::host_to_network_long(
          multicast_address.to_ulong());
    value_.imr_interface.s_addr =
      boost::asio::detail::socket_ops::host_to_network_long(
          boost::asio::ipv4::address::any().to_ulong());
  }

  // Construct with multicast address and address of local interface to use.
  multicast_request(const boost::asio::ipv4::address& multicast_address,
      const boost::asio::ipv4::address& local_address)
  {
    value_.imr_multiaddr.s_addr =
      boost::asio::detail::socket_ops::host_to_network_long(
          multicast_address.to_ulong());
    value_.imr_interface.s_addr =
      boost::asio::detail::socket_ops::host_to_network_long(
          local_address.to_ulong());
  }

  // Get the level of the socket option.
  int level() const
  {
    return Level;
  }

  // Get the name of the socket option.
  int name() const
  {
    return Name;
  }

  // Get the address of the option data.
  ip_mreq* data()
  {
    return &value_;
  }

  // Get the address of the option data.
  const ip_mreq* data() const
  {
    return &value_;
  }

  // Get the size of the option data.
  std::size_t size() const
  {
    return sizeof(value_);
  }

private:
  ip_mreq value_;
};

} // namespace socket_option
} // namespace detail
} // namespace ipv4
} // namespace asio
} // namespace boost

#include <boost/asio/detail/pop_options.hpp>

#endif // BOOST_ASIO_IPV4_DETAIL_SOCKET_OPTION_HPP