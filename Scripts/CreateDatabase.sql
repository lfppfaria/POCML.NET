create schema machine_learning;

use machine_learning;

create table input_data(
	id int primary key auto_increment,
	sell_year int not null,
	sell_month int not null,
    `value` numeric(10, 2));
